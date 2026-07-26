using HomeAutomation.Core.Enums;
using HomeAutomation.Core.Services;
using HomeAutomation.Core.Tests.TestSupport;
using HomeAutomation.Database.Enums;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System.Net;
using System.Text.Json;

namespace HomeAutomation.Core.Tests.Services;

public class ZWaveAPIServiceTests
{
    [Fact]
    public async Task GetNodes_WithValidResponse_ReturnsMappedNodes()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.Respond(
            HttpStatusCode.OK,
            """
            [
              {
                "id": 7,
                "protocolInfo": { "basicType": 1, "genericType": 2, "specificType": 3 },
                "manufacturerSpecific": { "manufacturerId": "10", "typeId": "20", "productId": "30" },
                "supportedCommands": []
              }
            ]
            """);
        ZWaveAPIService service = CreateService(handler);

        HomeAutomation.Core.Models.ZWaveDeviceModel[] result =
            [.. await service.GetNodes(CancellationToken.None)];

        HomeAutomation.Core.Models.ZWaveDeviceModel node = Assert.Single(result);
        Assert.Equal(7, node.ID);
        Assert.Equal("10", node.ManufacturerSpecific.ManufacturerId);
        Assert.Equal(HttpMethod.Get, handler.LastMethod);
        Assert.Equal("https://zwave.test/api/nodes/", handler.LastRequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task GetNodes_WithEmptyResponse_ReturnsEmptyCollection()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.Respond(HttpStatusCode.OK);
        ZWaveAPIService service = CreateService(handler);

        IEnumerable<HomeAutomation.Core.Models.ZWaveDeviceModel> result =
            await service.GetNodes(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNodes_WithMissingConfiguration_ThrowsConfigurationError()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.Respond(HttpStatusCode.OK);
        ZWaveAPIService service = CreateService(handler, new Dictionary<string, string?>());

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetNodes(CancellationToken.None));

        Assert.Contains("ZWave:APIUrl", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SendCommand_EscapesParameterAndUsesPost()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.Respond(HttpStatusCode.NoContent);
        ZWaveAPIService service = CreateService(handler);

        bool result = await service.SendCommand(
            4,
            ZWaveCommandClass.Basic,
            "value with spaces",
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Equal(
            "https://zwave.test/api/nodes/4/send/Basic?parameter=value%20with%20spaces",
            handler.LastRequestUri?.AbsoluteUri);
    }

    [Theory]
    [InlineData(255, DeviceEvent.On)]
    [InlineData(0, DeviceEvent.Off)]
    [InlineData(1, DeviceEvent.Off)]
    public void ConvertParameterToEvent_WithBasicInteger_MapsState(int value, DeviceEvent expected)
    {
        ZWaveAPIService service = CreateService(StubHttpMessageHandler.Respond(HttpStatusCode.OK));

        DeviceEvent result = service.ConvertParameterToEvent(
            DeviceKind.PowerSwitch,
            ZWaveEventParameter.Basic,
            value);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertParameterToEvent_WithJsonBoolean_ReturnsUnknown()
    {
        using JsonDocument document = JsonDocument.Parse("true");
        ZWaveAPIService service = CreateService(StubHttpMessageHandler.Respond(HttpStatusCode.OK));

        DeviceEvent result = service.ConvertParameterToEvent(
            DeviceKind.PowerSwitch,
            ZWaveEventParameter.Basic,
            document.RootElement);

        Assert.Equal(DeviceEvent.Unknown, result);
    }

    [Fact]
    public void ConvertStateToCommand_WithUnknownState_ReturnsNoCommand()
    {
        ZWaveAPIService service = CreateService(StubHttpMessageHandler.Respond(HttpStatusCode.OK));

        ZWaveCommandClass? command = service.ConvertStateToCommand(DeviceEvent.Unknown, out object? parameter);

        Assert.Null(command);
        Assert.Null(parameter);
    }

    private static ZWaveAPIService CreateService(
        HttpMessageHandler handler,
        IReadOnlyDictionary<string, string?>? configurationValues = null)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues ?? new Dictionary<string, string?>
            {
                ["ZWave:APIUrl"] = "https://zwave.test/api"
            })
            .Build();

        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));
        return new ZWaveAPIService(configuration, httpClientFactory);
    }
}
