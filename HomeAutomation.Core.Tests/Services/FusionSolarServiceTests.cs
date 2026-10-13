using HomeAutomation.Core.Models;
using HomeAutomation.Core.Services;
using HomeAutomation.Core.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HomeAutomation.Core.Tests.Services;

public class FusionSolarServiceTests
{
    [Fact]
    public async Task GetEnergyFlow_AuthenticatesParsesFlowAndKeepsVirtualDeviceIds()
    {
        using FusionSolarFixture fixture = FusionSolarFixture.Create();

        FusionSolarEnergyFlowModel? flow = await fixture.Service.GetEnergyFlow();
        IEnumerable<FusionSolarDeviceModel> devices = await fixture.Service.GetDevices();

        Assert.NotNull(flow);
        Assert.Equal("station-123", flow.StationId);
        Assert.Equal(-0.75m, flow.GridFlow);
        Assert.Equal(-0.35m, flow.BatteryFlow);
        Assert.Equal(78m, flow.BatteryChargeLevel);
        Assert.Equal(1.25m, flow.HouseConsumption);
        Assert.Equal(2.5m, flow.SolarGeneration);
        Assert.Equal(DateTimeOffset.UtcNow.Offset, flow.Timestamp.Offset);
        Assert.Equal(
            ["station-123_panels", "station-123_battery", "station-123_grid", "station-123_house"],
            devices.Select(device => device.Id));
        Assert.Equal(
            ["FusionSolar - SolarCells", "FusionSolar - Battery", "FusionSolar - Grid", "FusionSolar - House"],
            devices.Select(device => device.Name));

        RecordedHttpRequest publicKeyRequest = Assert.Single(fixture.Handler.Requests, request => request.Uri.AbsolutePath == "/unisso/pubkey");
        Assert.Equal(HttpMethod.Get, publicKeyRequest.Method);
        RecordedHttpRequest loginRequest = Assert.Single(fixture.Handler.Requests, request => request.Uri.AbsolutePath == "/unisso/v3/validateUser.action");
        using JsonDocument loginPayload = JsonDocument.Parse(loginRequest.Body!);
        string encryptedPassword = loginPayload.RootElement.GetProperty("password").GetString()!;
        Assert.EndsWith("version-1", encryptedPassword, StringComparison.Ordinal);
        byte[] cipherText = Convert.FromBase64String(encryptedPassword[..^"version-1".Length]);
        Assert.Equal("fixture-password", Encoding.UTF8.GetString(fixture.PrivateKey.Decrypt(cipherText, RSAEncryptionPadding.OaepSHA384)));

        Assert.Contains(fixture.Handler.Requests, request =>
            request.Uri.AbsolutePath.EndsWith("energy-flow", StringComparison.Ordinal)
            && request.Headers["Cookie"].Contains("dp-session=fixture-session", StringComparison.Ordinal)
            && request.Headers["Roarand"] == "csrf-1");
    }

    [Fact]
    public async Task Authentication_AcceptsNumericTimestampAndNormalizedFieldNames()
    {
        using RSA publicKey = RSA.Create();
        string publicKeyPem = publicKey.ExportSubjectPublicKeyInfoPem();
        using FusionSolarFixture fixture = FusionSolarFixture.Create(request =>
            request.Uri.AbsolutePath == "/unisso/pubkey"
                ? FusionSolarFixture.Response(JsonSerializer.Serialize(new
                {
                    pub_key = publicKeyPem,
                    time_stamp = 1700000000123L,
                    version = "version-2"
                }))
                : null);

        await fixture.Service.GetDevices();

        RecordedHttpRequest loginRequest = Assert.Single(
            fixture.Handler.Requests,
            request => request.Uri.AbsolutePath == "/unisso/v3/validateUser.action");
        Assert.Contains("timeStamp=1700000000123", loginRequest.Uri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Authentication_ReportsSafeResponseShapeWhenTimestampIsUnavailable()
    {
        using RSA publicKey = RSA.Create();
        string publicKeyPem = publicKey.ExportSubjectPublicKeyInfoPem();
        using FusionSolarFixture fixture = FusionSolarFixture.Create(request =>
            request.Uri.AbsolutePath == "/unisso/pubkey"
                ? FusionSolarFixture.Response(JsonSerializer.Serialize(new
                {
                    pubKey = publicKeyPem,
                    code = "AUTH_ERROR",
                    message = "do-not-include-response-values"
                }))
                : null);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.GetDevices());

        Assert.Contains("timeStamp", exception.Message, StringComparison.Ordinal);
        Assert.Contains("pubKey, code, message", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("do-not-include-response-values", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetEnergyFlow_RefreshesExpiredCsrfTokenBeforeReading()
    {
        using FusionSolarFixture fixture = FusionSolarFixture.Create();
        await fixture.Service.GetEnergyFlow();
        fixture.Clock.Advance(TimeSpan.FromMinutes(6));

        await fixture.Service.GetEnergyFlow();

        Assert.Equal(2, fixture.Handler.Requests.Count(request => request.Uri.AbsolutePath.EndsWith("keep-alive", StringComparison.Ordinal)));
        Assert.Contains(fixture.Handler.Requests, request =>
            request.Uri.AbsolutePath.EndsWith("energy-flow", StringComparison.Ordinal)
            && request.Headers["Roarand"] == "csrf-2");
        Assert.Single(fixture.Handler.Requests, request => request.Uri.AbsolutePath == "/unisso/pubkey");
    }

    [Fact]
    public async Task GetEnergyFlow_RenewsExpiredSessionAndRetriesReadOnce()
    {
        int energyFlowRequests = 0;
        using FusionSolarFixture fixture = FusionSolarFixture.Create(request =>
        {
            if (request.Uri.AbsolutePath.EndsWith("energy-flow", StringComparison.Ordinal)
                && energyFlowRequests++ == 0)
            {
                return FusionSolarFixture.Response("{}", HttpStatusCode.Unauthorized);
            }

            return null;
        });

        FusionSolarEnergyFlowModel? flow = await fixture.Service.GetEnergyFlow();

        Assert.NotNull(flow);
        Assert.Equal(2, energyFlowRequests);
        Assert.Equal(2, fixture.Handler.Requests.Count(request => request.Uri.AbsolutePath == "/unisso/pubkey"));
    }

    [Fact]
    public async Task SessionRequests_AreSerializedAcrossPollingAndConfigurationCalls()
    {
        using FusionSolarFixture fixture = FusionSolarFixture.Create();

        await Task.WhenAll(
            fixture.Service.GetDevices(),
            fixture.Service.GetEnergyFlow(),
            fixture.Service.SetConfigSignals("dongle", new { id = "230190100", value = "5" }, CancellationToken.None),
            fixture.Service.GetEnergyFlow());

        Assert.Single(fixture.Handler.Requests, request => request.Uri.AbsolutePath == "/unisso/pubkey");
        Assert.Single(fixture.Handler.Requests, request => request.Uri.AbsolutePath.EndsWith("station-list", StringComparison.Ordinal));
        Assert.Equal(2, fixture.Handler.Requests.Count(request => request.Uri.AbsolutePath.EndsWith("energy-flow", StringComparison.Ordinal)));
        Assert.Single(fixture.Handler.Requests, request => request.Uri.AbsolutePath.EndsWith("set-config-signals", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SetConfigSignals_SendsModeBeforeTimetableWithEncodedDongleTarget()
    {
        using FusionSolarFixture fixture = FusionSolarFixture.Create();
        object[] changes =
        [
            new { id = "230190101", value = "[{\"startTime\":\"00:00\"}]" },
            new { id = "230190100", value = "5" }
        ];

        bool success = await fixture.Service.SetConfigSignals("dongle", changes, CancellationToken.None);

        Assert.True(success);
        RecordedHttpRequest request = Assert.Single(fixture.Handler.Requests, item => item.Uri.AbsolutePath.EndsWith("set-config-signals", StringComparison.Ordinal));
        Dictionary<string, string> form = ParseForm(request.Body!);
        Assert.Equal("NE=station-123/dongle&device=1", form["dn"]);
        using JsonDocument payload = JsonDocument.Parse(form["changeValues"]);
        Assert.Equal("230190100", payload.RootElement[0].GetProperty("id").GetString());
        Assert.Equal("5", payload.RootElement[0].GetProperty("value").GetString());
        Assert.Equal("230190101", payload.RootElement[1].GetProperty("id").GetString());
        Assert.Equal("application/x-www-form-urlencoded", request.Headers["Content-Type"]);
        Assert.Equal("csrf-1", request.Headers["Roarand"]);
    }

    [Fact]
    public async Task SetConfigSignals_PreservesBatteryTargetAndDoesNotRetryAmbiguousWrite()
    {
        int writes = 0;
        using FusionSolarFixture fixture = FusionSolarFixture.Create(request =>
        {
            if (request.Uri.AbsolutePath.EndsWith("set-config-signals", StringComparison.Ordinal))
            {
                writes++;
                throw new HttpRequestException("fixture network failure");
            }

            return null;
        });

        bool success = await fixture.Service.SetConfigSignals(
            "battery",
            new { id = 230320278, value = 10 },
            CancellationToken.None);

        Assert.False(success);
        RecordedHttpRequest request = Assert.Single(fixture.Handler.Requests, item => item.Uri.AbsolutePath.EndsWith("set-config-signals", StringComparison.Ordinal));
        Assert.Equal(1, writes);
        Assert.Equal("NE=station-123/battery&device=2", ParseForm(request.Body!)["dn"]);
        using JsonDocument changes = JsonDocument.Parse(ParseForm(request.Body!)["changeValues"]);
        Assert.Equal(JsonValueKind.Number, changes.RootElement[0].GetProperty("id").ValueKind);
        Assert.Equal(JsonValueKind.Number, changes.RootElement[0].GetProperty("value").ValueKind);
    }

    [Fact]
    public async Task SetConfigSignals_TimetableOnlyDoesNotAddModeAndChecksEveryDeviceResult()
    {
        using FusionSolarFixture fixture = FusionSolarFixture.Create(request =>
            request.Uri.AbsolutePath.EndsWith("set-config-signals", StringComparison.Ordinal)
                ? FusionSolarFixture.Response("{\"code\":0,\"data\":[{\"code\":0},{\"code\":43}]}")
                : null);

        bool success = await fixture.Service.SetConfigSignals(
            "dongle",
            new { id = "230190101", value = "[]" },
            CancellationToken.None);

        Assert.False(success);
        RecordedHttpRequest request = Assert.Single(fixture.Handler.Requests, item => item.Uri.AbsolutePath.EndsWith("set-config-signals", StringComparison.Ordinal));
        using JsonDocument changes = JsonDocument.Parse(ParseForm(request.Body!)["changeValues"]);
        Assert.Single(changes.RootElement.EnumerateArray());
        Assert.Equal("230190101", changes.RootElement[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task SetConfigSignals_MalformedUpstreamResponseIsFailure()
    {
        using FusionSolarFixture fixture = FusionSolarFixture.Create(request =>
            request.Uri.AbsolutePath.EndsWith("set-config-signals", StringComparison.Ordinal)
                ? FusionSolarFixture.Response("{\"code\":0,\"data\":[]}")
                : null);

        bool success = await fixture.Service.SetConfigSignals(
            "dongle",
            new { id = "230190100", value = "5" },
            CancellationToken.None);

        Assert.False(success);
    }

    [Fact]
    public async Task SetConfigSignals_MissingPhysicalDeviceFailsWithoutSendingAWrite()
    {
        using FusionSolarFixture fixture = FusionSolarFixture.Create(request =>
            request.Uri.AbsolutePath.EndsWith("/device-list", StringComparison.Ordinal)
                ? FusionSolarFixture.Response("{\"code\":0,\"data\":[]}")
                : null);

        IEnumerable<FusionSolarDeviceModel> devices = await fixture.Service.GetDevices();
        bool success = await fixture.Service.SetConfigSignals(
            "dongle",
            new { id = "230190100", value = "5" },
            CancellationToken.None);

        Assert.Equal(4, devices.Count());
        Assert.False(success);
        Assert.DoesNotContain(fixture.Handler.Requests, request => request.Uri.AbsolutePath.EndsWith("set-config-signals", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SetConfigSignals_RejectsMalformedSignalsBeforeAuthenticating()
    {
        using FusionSolarFixture fixture = FusionSolarFixture.Create();

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Service.SetConfigSignals(
            "dongle",
            Array.Empty<object>(),
            CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Service.SetConfigSignals(
            "dongle",
            new { id = "", value = true },
            CancellationToken.None));

        Assert.Empty(fixture.Handler.Requests);
    }

    [Fact]
    public async Task GetEnergyFlow_MalformedFlowDoesNotReturnReadings()
    {
        using FusionSolarFixture fixture = FusionSolarFixture.Create(request =>
            request.Uri.AbsolutePath.EndsWith("energy-flow", StringComparison.Ordinal)
                ? FusionSolarFixture.Response("{\"data\":{\"flow\":{\"nodes\":[],\"links\":\"invalid\"}}}")
                : null);

        FusionSolarEnergyFlowModel? flow = await fixture.Service.GetEnergyFlow();

        Assert.Null(flow);
    }

    [Fact]
    public async Task GetEnergyFlow_EmptyFlowDoesNotInventZeroReadings()
    {
        using FusionSolarFixture fixture = FusionSolarFixture.Create(request =>
            request.Uri.AbsolutePath.EndsWith("energy-flow", StringComparison.Ordinal)
                ? FusionSolarFixture.Response("{\"data\":{\"flow\":{\"nodes\":[],\"links\":[]}}}")
                : null);

        FusionSolarEnergyFlowModel? flow = await fixture.Service.GetEnergyFlow();

        Assert.Null(flow);
    }

    private static Dictionary<string, string> ParseForm(string body)
    {
        return body.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                part => WebUtility.UrlDecode(part[0]),
                part => WebUtility.UrlDecode(part.ElementAtOrDefault(1) ?? string.Empty),
                StringComparer.Ordinal);
    }

    private sealed class FusionSolarFixture : IDisposable
    {
        private static readonly string EnergyFlowJson = """
            {
              "code": 0,
              "data": {
                "flow": {
                  "nodes": [
                    { "name": "neteco.pvms.KPI.kpiView.electricalLoad", "value": "1.25 kW" },
                    { "name": "neteco.pvms.devTypeLangKey.energy_store", "deviceTips": { "BATTERY_POWER": "-0.35 kW", "SOC": "78 %" } },
                    { "name": "neteco.pvms.devTypeLangKey.string", "value": "2.50 kW" }
                  ],
                  "links": [
                    { "description": { "label": "neteco.pvms.energy.flow.buy.power", "value": "0.75 kW" }, "toNode": "2" }
                  ]
                }
              }
            }
            """;

        private readonly Func<RecordedHttpRequest, HttpResponseMessage?>? intercept;
        private int keepAliveCount;

        private FusionSolarFixture(Func<RecordedHttpRequest, HttpResponseMessage?>? intercept)
        {
            this.intercept = intercept;
            PrivateKey = RSA.Create(2048);
            Handler = new RecordingHttpMessageHandler(RespondAsync);
            Client = new HttpClient(Handler);
            IHttpClientFactory clientFactory = Substitute.For<IHttpClientFactory>();
            clientFactory.CreateClient(nameof(FusionSolarService)).Returns(Client);

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FusionSolar:Host"] = "login.fusionsolar.test",
                    ["FusionSolar:Username"] = "fixture-user",
                    ["FusionSolar:Password"] = "fixture-password"
                })
                .Build();
            Service = new FusionSolarService(
                configuration,
                clientFactory,
                NullLogger<FusionSolarService>.Instance,
                Clock);
        }

        public RSA PrivateKey { get; }
        public RecordingHttpMessageHandler Handler { get; }
        private HttpClient Client { get; }
        public FusionSolarService Service { get; }
        public MutableTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero));

        public static FusionSolarFixture Create(Func<RecordedHttpRequest, HttpResponseMessage?>? intercept = null)
        {
            return new FusionSolarFixture(intercept);
        }

        public static HttpResponseMessage Response(string body, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            };
        }

        private async Task<HttpResponseMessage> RespondAsync(RecordedHttpRequest request, CancellationToken cancellationToken)
        {
            HttpResponseMessage? intercepted = intercept?.Invoke(request);
            if (intercepted is not null)
                return intercepted;

            if (request.Uri.AbsolutePath == "/unisso/pubkey")
            {
                string pem = PrivateKey.ExportSubjectPublicKeyInfoPem();
                return Response(JsonSerializer.Serialize(new { pubKey = pem, timeStamp = "fixture-time", version = "version-1" }));
            }

            if (request.Uri.AbsolutePath == "/unisso/v3/validateUser.action")
                return Response("{\"redirectURL\":\"/region\"}");

            if (request.Uri.AbsolutePath == "/region")
            {
                HttpResponseMessage regionalResponse = new(HttpStatusCode.Found)
                {
                    Content = new StringContent(string.Empty)
                };
                regionalResponse.Headers.Location = new Uri("https://data.fusionsolar.test/uniportal/");
                regionalResponse.Headers.TryAddWithoutValidation("Set-Cookie", "dp-session=fixture-session; Path=/; Secure; HttpOnly");
                return regionalResponse;
            }

            if (request.Uri.AbsolutePath.EndsWith("keep-alive", StringComparison.Ordinal))
            {
                keepAliveCount++;
                return Response(JsonSerializer.Serialize(new { code = 0, payload = $"csrf-{keepAliveCount}" }));
            }

            if (request.Uri.AbsolutePath.EndsWith("station-list", StringComparison.Ordinal))
                return Response("{\"code\":0,\"data\":{\"list\":[{\"dn\":\"station-123\",\"batteryCapacity\":10}]}}");

            if (request.Uri.AbsolutePath.EndsWith("/device-list", StringComparison.Ordinal))
                return Response("{\"code\":0,\"data\":[{\"dn\":\"NE=station-123/dongle&device=1\",\"mocTypeName\":\"Dongle\"},{\"dn\":\"NE=station-123/inverter&device=1\",\"mocTypeName\":\"Inverter\"}]}");

            if (request.Uri.AbsolutePath.EndsWith("children-list", StringComparison.Ordinal))
                return Response("{\"code\":0,\"data\":[{\"dn\":\"NE=station-123/battery&device=2\",\"mocTypeName\":\"Battery\"}]}");

            if (request.Uri.AbsolutePath.EndsWith("energy-flow", StringComparison.Ordinal))
                return Response(EnergyFlowJson);

            if (request.Uri.AbsolutePath.EndsWith("set-config-signals", StringComparison.Ordinal))
                return Response("{\"code\":0,\"data\":[{\"code\":0}]}");

            return Response("{}", HttpStatusCode.NotFound);
        }

        public void Dispose()
        {
            Service.Dispose();
            Client.Dispose();
            PrivateKey.Dispose();
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset currentTime = now;

        public override DateTimeOffset GetUtcNow() => currentTime;

        public void Advance(TimeSpan amount) => currentTime = currentTime.Add(amount);
    }
}
