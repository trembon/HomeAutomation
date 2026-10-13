using HomeAutomation.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeAutomation.Core.Services;

public interface IFusionSolarService
{
    Task<IEnumerable<FusionSolarDeviceModel>> GetDevices(CancellationToken cancellationToken = default);
    Task<FusionSolarEnergyFlowModel?> GetEnergyFlow(CancellationToken cancellationToken = default);
    Task<bool> SetConfigSignals(string type, object payload, CancellationToken cancellationToken);
}

public class FusionSolarService(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<FusionSolarService> logger,
    TimeProvider timeProvider) : IFusionSolarService, IDisposable
{
    private const string RefererPath = "/uniportal/pvmswebsite/assets/build/cloud.html";
    private static readonly TimeSpan CsrfTokenLifetime = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private FusionSolarSession? _session;

    public void Dispose()
    {
        _sessionLock.Dispose();
        GC.SuppressFinalize(this);
    }

    public Task<IEnumerable<FusionSolarDeviceModel>> GetDevices(CancellationToken cancellationToken = default)
    {
        return ExecuteWithSessionAsync((session, _) => Task.FromResult<IEnumerable<FusionSolarDeviceModel>>(
        [
            new() { Id = $"{session.StationId}_panels", Name = "FusionSolar - SolarCells" },
            new() { Id = $"{session.StationId}_battery", Name = "FusionSolar - Battery" },
            new() { Id = $"{session.StationId}_grid", Name = "FusionSolar - Grid" },
            new() { Id = $"{session.StationId}_house", Name = "FusionSolar - House" }
        ]), cancellationToken);
    }

    private async Task<FusionSolarEnergyFlowModel?> GetEnergyFlowCoreAsync(
        FusionSolarSession session,
        CancellationToken cancellationToken)
    {
        using JsonDocument document = await SendReadJsonAsync(
            session,
            HttpMethod.Get,
            "/rest/pvms/web/station/v2/overview/energy-flow",
            BuildQuery([("stationDn", Uri.UnescapeDataString(session.StationId))]),
            null,
            cancellationToken);
        if (!TryGetProperty(document.RootElement, "data", out JsonElement data)
            || !TryGetProperty(data, "flow", out JsonElement flow)
            || flow.ValueKind != JsonValueKind.Object
            || !TryGetProperty(flow, "nodes", out JsonElement nodes)
            || nodes.ValueKind != JsonValueKind.Array
            || !TryGetProperty(flow, "links", out JsonElement links)
            || links.ValueKind != JsonValueKind.Array)
        {
            logger.LogWarning("FusionSolar.EnergyFlow :: response did not contain a valid energy flow");
            return null;
        }

        decimal gridFlow = 0m;
        decimal batteryFlow = 0m;
        decimal batteryChargeLevel = 0m;
        decimal houseConsumption = 0m;
        decimal solarGeneration = 0m;
        bool hasRecognizedReading = false;

        foreach (JsonElement node in nodes.EnumerateArray())
        {
            string? name = GetOptionalString(node, "name");
            if (string.Equals(name, "neteco.pvms.KPI.kpiView.electricalLoad", StringComparison.Ordinal))
            {
                if (!TryGetDecimal(node, "value", out houseConsumption))
                    return null;
                hasRecognizedReading = true;
            }
            else if (string.Equals(name, "neteco.pvms.devTypeLangKey.energy_store", StringComparison.Ordinal))
            {
                if (!TryGetProperty(node, "deviceTips", out JsonElement deviceTips)
                    || !TryGetDecimal(deviceTips, "BATTERY_POWER", out batteryFlow)
                    || !TryGetDecimal(deviceTips, "SOC", out batteryChargeLevel))
                {
                    return null;
                }

                hasRecognizedReading = true;
            }
            else if (string.Equals(name, "neteco.pvms.devTypeLangKey.string", StringComparison.Ordinal)
                && !TryGetDecimal(node, "value", out solarGeneration))
            {
                return null;
            }
            else if (string.Equals(name, "neteco.pvms.devTypeLangKey.string", StringComparison.Ordinal))
            {
                hasRecognizedReading = true;
            }
        }

        foreach (JsonElement link in links.EnumerateArray())
        {
            if (!TryGetProperty(link, "description", out JsonElement description)
                || !string.Equals(GetOptionalString(description, "label"), "neteco.pvms.energy.flow.buy.power", StringComparison.Ordinal))
            {
                continue;
            }

            if (!TryGetDecimal(description, "value", out gridFlow))
                return null;

            hasRecognizedReading = true;
            if (string.Equals(GetOptionalString(link, "toNode"), "2", StringComparison.Ordinal))
                gridFlow = -gridFlow;
        }

        if (!hasRecognizedReading)
        {
            logger.LogWarning("FusionSolar.EnergyFlow :: response did not contain any recognized measurements");
            return null;
        }

        return new FusionSolarEnergyFlowModel
        {
            StationId = session.StationId,
            Timestamp = timeProvider.GetUtcNow(),
            GridFlow = gridFlow,
            BatteryFlow = batteryFlow,
            BatteryChargeLevel = batteryChargeLevel,
            HouseConsumption = houseConsumption,
            SolarGeneration = solarGeneration
        };
    }

    public Task<FusionSolarEnergyFlowModel?> GetEnergyFlow(CancellationToken cancellationToken = default)
    {
        return ExecuteWithSessionAsync(GetEnergyFlowCoreAsync, cancellationToken);
    }

    public async Task<bool> SetConfigSignals(string type, object payload, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(payload);

        List<ConfigSignalChange> changes = NormalizeConfigSignals(payload);
        if (string.Equals(type, "dongle", StringComparison.OrdinalIgnoreCase))
        {
            int modeIndex = changes.ToList().FindIndex(change => SignalIdIs(change, "230190100"));
            int timetableIndex = changes.ToList().FindIndex(change => SignalIdIs(change, "230190101"));
            if (modeIndex > timetableIndex && timetableIndex >= 0)
            {
                List<ConfigSignalChange> orderedChanges = [.. changes];
                ConfigSignalChange mode = orderedChanges[modeIndex];
                orderedChanges.RemoveAt(modeIndex);
                orderedChanges.Insert(timetableIndex, mode);
                changes = orderedChanges;
            }
        }

        await _sessionLock.WaitAsync(cancellationToken);
        try
        {
            FusionSolarSession session = await GetSessionAsync(cancellationToken);
            if (!await EnsureCsrfTokenAsync(session, cancellationToken))
            {
                _session = null;
                session = await GetSessionAsync(cancellationToken);
                if (!await EnsureCsrfTokenAsync(session, cancellationToken))
                {
                    logger.LogError("FusionSolar.Configuration :: failed to refresh the security token");
                    return false;
                }
            }

            string? deviceDn = string.Equals(type, "dongle", StringComparison.OrdinalIgnoreCase)
                ? session.DongleDn
                : session.BatteryDn;
            if (string.IsNullOrWhiteSpace(deviceDn))
            {
                logger.LogError("FusionSolar.Configuration :: the selected {deviceType} device is unavailable", type);
                return false;
            }

            string serializedChanges = JsonSerializer.Serialize(changes, JsonOptions);
            using FormUrlEncodedContent content = new(
            [
                new KeyValuePair<string, string>("dn", deviceDn),
                new KeyValuePair<string, string>("changeValues", serializedChanges)
            ]);
            using HttpRequestMessage request = CreateSessionRequest(
                session,
                HttpMethod.Post,
                "/rest/pvms/web/device/v1/deviceExt/set-config-signals",
                includeCsrf: true);
            request.Content = content;

            HttpClient client = httpClientFactory.CreateClient(nameof(FusionSolarService));
            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("FusionSolar.Configuration :: request failed with HTTP {statusCode}", (int)response.StatusCode);
                return false;
            }

            using JsonDocument result = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);
            if (!TryGetProperty(result.RootElement, "code", out JsonElement responseCode)
                || responseCode.ValueKind != JsonValueKind.Number
                || !responseCode.TryGetInt32(out int code))
            {
                logger.LogError("FusionSolar.Configuration :: response did not contain a valid result code");
                return false;
            }

            if (code != 0)
            {
                logger.LogError("FusionSolar.Configuration :: FusionSolar rejected the request with code {upstreamCode}", code);
                return false;
            }

            if (!TryGetProperty(result.RootElement, "data", out JsonElement deviceResults)
                || deviceResults.ValueKind != JsonValueKind.Array
                || deviceResults.GetArrayLength() == 0)
            {
                logger.LogError("FusionSolar.Configuration :: response did not contain device results");
                return false;
            }

            foreach (JsonElement deviceResult in deviceResults.EnumerateArray())
            {
                if (!TryGetProperty(deviceResult, "code", out JsonElement deviceCode)
                    || deviceCode.ValueKind != JsonValueKind.Number
                    || !deviceCode.TryGetInt32(out int deviceResultCode))
                {
                    logger.LogError("FusionSolar.Configuration :: response contained an invalid device result");
                    return false;
                }

                if (deviceResultCode != 0)
                {
                    logger.LogError("FusionSolar.Configuration :: FusionSolar rejected a device update with code {upstreamCode}", deviceResultCode);
                    return false;
                }
            }

            return true;
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "FusionSolar.Configuration :: request failed");
            return false;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "FusionSolar.Configuration :: request timed out; its result is unknown and was not retried");
            return false;
        }
        catch (FusionSolarSessionExpiredException)
        {
            logger.LogError("FusionSolar.Configuration :: could not establish a valid session before sending the update");
            return false;
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "FusionSolar.Configuration :: response was not valid JSON");
            return false;
        }
        catch (InvalidOperationException exception)
        {
            logger.LogError(exception, "FusionSolar.Configuration :: FusionSolar is not configured or available");
            return false;
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    private async Task<T> ExecuteWithSessionAsync<T>(
        Func<FusionSolarSession, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        await _sessionLock.WaitAsync(cancellationToken);
        try
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                FusionSolarSession session = await GetSessionAsync(cancellationToken);
                try
                {
                    return await operation(session, cancellationToken);
                }
                catch (FusionSolarSessionExpiredException) when (attempt == 0)
                {
                    _session = null;
                    logger.LogWarning("FusionSolar.Session :: session expired; renewing before retrying the read");
                }
            }

            throw new InvalidOperationException("FusionSolar read retry ended without a result.");
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    private async Task<FusionSolarSession> GetSessionAsync(CancellationToken cancellationToken)
    {
        if (_session is not null)
            return _session;

        _session = await AuthenticateAsync(cancellationToken);
        return _session;
    }

    private async Task<FusionSolarSession> AuthenticateAsync(CancellationToken cancellationToken)
    {
        Uri loginHost = GetLoginHost();
        string username = configuration["FusionSolar:Username"]?.Trim() ?? string.Empty;
        string password = configuration["FusionSolar:Password"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("FusionSolar:Username and FusionSolar:Password must be configured.");

        HttpClient client = httpClientFactory.CreateClient(nameof(FusionSolarService));
        using HttpResponseMessage publicKeyResponse = await client.GetAsync(
            new Uri(loginHost, "/unisso/pubkey"),
            cancellationToken);
        publicKeyResponse.EnsureSuccessStatusCode();
        using JsonDocument publicKeyDocument = await JsonDocument.ParseAsync(
            await publicKeyResponse.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);
        string publicKeyPem = GetRequiredString(publicKeyDocument.RootElement, "pubKey", publicKeyResponse);
        string timestamp = GetRequiredScalarString(publicKeyDocument.RootElement, "timeStamp", publicKeyResponse);
        string version = GetRequiredString(publicKeyDocument.RootElement, "version", publicKeyResponse);

        string nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        string encryptedPassword = Convert.ToBase64String(
            rsa.Encrypt(Encoding.UTF8.GetBytes(password), RSAEncryptionPadding.OaepSHA384)) + version;

        Uri loginUri = new(loginHost, $"/unisso/v3/validateUser.action?timeStamp={Uri.EscapeDataString(timestamp)}&nonce={nonce}");
        using HttpRequestMessage loginRequest = new(HttpMethod.Post, loginUri)
        {
            Content = JsonContent(new
            {
                organizationName = string.Empty,
                password = encryptedPassword,
                username
            })
        };
        loginRequest.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
        loginRequest.Headers.TryAddWithoutValidation("Origin", loginHost.GetLeftPart(UriPartial.Authority));
        loginRequest.Headers.Referrer = new Uri(loginHost, "/unisso/login.action");
        using HttpResponseMessage loginResponse = await client.SendAsync(loginRequest, cancellationToken);
        loginResponse.EnsureSuccessStatusCode();
        using JsonDocument loginDocument = await JsonDocument.ParseAsync(
            await loginResponse.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);
        Uri regionalLoginUri = ResolveRegionalLoginUri(loginDocument.RootElement, loginHost);

        using HttpRequestMessage regionalRequest = new(HttpMethod.Get, regionalLoginUri);
        regionalRequest.Headers.Referrer = new Uri(loginHost, "/pvmswebsite/loginCustomize.html");
        using HttpResponseMessage regionalResponse = await client.SendAsync(regionalRequest, cancellationToken);
        if (regionalResponse.StatusCode is not HttpStatusCode.OK and not HttpStatusCode.Found and not HttpStatusCode.MovedPermanently)
            regionalResponse.EnsureSuccessStatusCode();

        string? location = regionalResponse.Headers.Location?.OriginalString;
        if (string.IsNullOrWhiteSpace(location))
            throw new InvalidOperationException("FusionSolar regional sign-in response did not include a data host.");

        Uri dataHost = new(location, UriKind.Absolute);
        if (dataHost.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(dataHost.Host))
            throw new InvalidOperationException("FusionSolar regional sign-in returned an invalid data host.");

        string? sessionCookie = null;
        if (regionalResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieHeaders))
        {
            foreach (string cookieHeader in cookieHeaders)
            {
                string firstPart = cookieHeader.Split(';', 2)[0].Trim();
                const string cookiePrefix = "dp-session=";
                if (firstPart.StartsWith(cookiePrefix, StringComparison.Ordinal))
                    sessionCookie = firstPart[cookiePrefix.Length..];
            }
        }

        if (string.IsNullOrWhiteSpace(sessionCookie))
            throw new InvalidOperationException("FusionSolar regional sign-in did not return a session cookie.");

        FusionSolarSession session = new(dataHost, sessionCookie);
        if (!await EnsureCsrfTokenAsync(session, cancellationToken))
            throw new FusionSolarSessionExpiredException();

        using JsonDocument stationDocument = await SendReadJsonAsync(
            session,
            HttpMethod.Post,
            "/rest/pvms/web/station/v1/station/station-list",
            null,
            new
            {
                curPage = 1,
                pageSize = 10,
                gridConnectedTime = string.Empty,
                queryTime = 1666044000000L,
                timeZone = 2,
                sortId = "createTime",
                sortDir = "DESC",
                locale = "en_US"
            },
            cancellationToken);
        if (!TryGetProperty(stationDocument.RootElement, "data", out JsonElement stationData)
            || !TryGetProperty(stationData, "list", out JsonElement stationList)
            || stationList.ValueKind != JsonValueKind.Array
            || stationList.GetArrayLength() == 0
            || !TryGetProperty(stationList[0], "dn", out JsonElement stationDn)
            || stationDn.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(stationDn.GetString()))
        {
            throw new InvalidOperationException("FusionSolar did not return a solar installation.");
        }

        session.StationId = stationDn.GetString()!;
        await TryDiscoverConfigurationDevicesAsync(session, cancellationToken);
        return session;
    }

    private async Task TryDiscoverConfigurationDevicesAsync(FusionSolarSession session, CancellationToken cancellationToken)
    {
        try
        {
            string stationId = Uri.UnescapeDataString(session.StationId);
            string query = BuildQuery(
            [
                ("conditionParams.checkShareStationDn", stationId),
                ("conditionParams.parentDn", stationId),
                ("conditionParams.curPage", "1"),
                ("conditionParams.recordperpage", "10"),
                ("conditionParams.sortType", "BY_DEVICE_NAME"),
                ("conditionParams.maintenance", "false")
            ]);
            using JsonDocument deviceDocument = await SendReadJsonAsync(
                session,
                HttpMethod.Get,
                "/rest/neteco/web/config/device/v1/device-list",
                query,
                null,
                cancellationToken);

            if (!TryGetProperty(deviceDocument.RootElement, "data", out JsonElement devices)
                || devices.ValueKind != JsonValueKind.Array)
            {
                logger.LogWarning("FusionSolar.Devices :: physical device list was unavailable");
                return;
            }

            string? inverterDn = null;
            foreach (JsonElement device in devices.EnumerateArray())
            {
                string? kind = GetOptionalString(device, "mocTypeName");
                string? dn = GetOptionalString(device, "dn");
                if (string.IsNullOrWhiteSpace(dn))
                    continue;

                if (string.Equals(kind, "Dongle", StringComparison.Ordinal))
                    session.DongleDn = dn;
                else if (string.Equals(kind, "Inverter", StringComparison.Ordinal))
                    inverterDn ??= dn;
            }

            if (inverterDn is null)
                return;

            string childQuery = BuildQuery(
            [
                ("conditionParams.parentDn", Uri.UnescapeDataString(inverterDn)),
                ("conditionParams.curPage", "0"),
                ("conditionParams.recordperpage", "500"),
                ("conditionParams.signals", "20010,50012,10008,10007,10009,19999,10041,20004,50009,50010"),
                ("conditionParams.mocTypes", "20814,20815,20816,50017,60022,60066,60014,60015,23037,20868")
            ]);
            using JsonDocument childrenDocument = await SendReadJsonAsync(
                session,
                HttpMethod.Get,
                "/rest/neteco/web/config/device/v1/children-list",
                childQuery,
                null,
                cancellationToken);
            if (!TryGetProperty(childrenDocument.RootElement, "data", out JsonElement children)
                || children.ValueKind != JsonValueKind.Array)
            {
                logger.LogWarning("FusionSolar.Devices :: battery device list was unavailable");
                return;
            }

            foreach (JsonElement child in children.EnumerateArray())
            {
                if (string.Equals(GetOptionalString(child, "mocTypeName"), "Battery", StringComparison.Ordinal))
                {
                    session.BatteryDn = GetOptionalString(child, "dn");
                    break;
                }
            }
        }
        catch (FusionSolarSessionExpiredException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException)
        {
            logger.LogWarning(exception, "FusionSolar.Devices :: physical configuration devices could not be discovered");
        }
    }

    private async Task<JsonDocument> SendReadJsonAsync(
        FusionSolarSession session,
        HttpMethod method,
        string path,
        string? query,
        object? body,
        CancellationToken cancellationToken)
    {
        if (!await EnsureCsrfTokenAsync(session, cancellationToken))
            throw new FusionSolarSessionExpiredException();

        using HttpRequestMessage request = CreateSessionRequest(session, method, path, includeCsrf: true, query);
        if (body is not null)
            request.Content = JsonContent(body);

        HttpClient client = httpClientFactory.CreateClient(nameof(FusionSolarService));
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new FusionSolarSessionExpiredException();

        response.EnsureSuccessStatusCode();
        JsonDocument document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        if (TryGetProperty(document.RootElement, "code", out JsonElement code)
            && code.ValueKind == JsonValueKind.Number
            && code.TryGetInt32(out int numericCode)
            && numericCode != 0)
        {
            document.Dispose();
            throw new FusionSolarSessionExpiredException();
        }

        return document;
    }

    private async Task<bool> EnsureCsrfTokenAsync(FusionSolarSession session, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(session.CsrfToken)
            && timeProvider.GetUtcNow() - session.CsrfUpdatedAt <= CsrfTokenLifetime)
        {
            return true;
        }

        try
        {
            using HttpRequestMessage request = new(
                HttpMethod.Get,
                new Uri(session.DataHost, "/rest/dpcloud/auth/v1/keep-alive"));
            request.Headers.TryAddWithoutValidation("Cookie", session.CookieHeader);
            request.Headers.Referrer = new Uri(session.DataHost, RefererPath);
            HttpClient client = httpClientFactory.CreateClient(nameof(FusionSolarService));
            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return false;

            using JsonDocument document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);
            if (TryGetProperty(document.RootElement, "code", out JsonElement code)
                && code.ValueKind == JsonValueKind.Number
                && code.TryGetInt32(out int numericCode)
                && numericCode == 0
                && TryGetProperty(document.RootElement, "payload", out JsonElement payload)
                && payload.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(payload.GetString()))
            {
                session.CsrfToken = payload.GetString();
                session.CsrfUpdatedAt = timeProvider.GetUtcNow();
                return true;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            logger.LogWarning(exception, "FusionSolar.Session :: security token refresh failed");
        }

        return false;
    }

    private static HttpRequestMessage CreateSessionRequest(
        FusionSolarSession session,
        HttpMethod method,
        string path,
        bool includeCsrf,
        string? query = null)
    {
        string uri = path;
        if (!string.IsNullOrEmpty(query))
            uri += $"?{query}";

        HttpRequestMessage request = new(method, new Uri(session.DataHost, uri));
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.TryAddWithoutValidation("Accept-Language", "en-GB,en;q=0.9");
        request.Headers.TryAddWithoutValidation("Cookie", session.CookieHeader);
        request.Headers.Referrer = new Uri(session.DataHost, RefererPath);
        if (includeCsrf && !string.IsNullOrWhiteSpace(session.CsrfToken))
            request.Headers.TryAddWithoutValidation("Roarand", session.CsrfToken);
        if (method != HttpMethod.Get)
            request.Headers.TryAddWithoutValidation("Origin", session.DataHost.GetLeftPart(UriPartial.Authority));
        return request;
    }

    private Uri GetLoginHost()
    {
        string? configuredHost = configuration["FusionSolar:Host"]?.Trim();
        if (string.IsNullOrWhiteSpace(configuredHost))
            throw new InvalidOperationException("FusionSolar:Host must be configured.");

        string hostWithScheme = configuredHost.Contains("://", StringComparison.Ordinal)
            ? configuredHost
            : $"https://{configuredHost}";
        if (!Uri.TryCreate(hostWithScheme, UriKind.Absolute, out Uri? host)
            || host.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(host.Host)
            || host.AbsolutePath != "/"
            || !string.IsNullOrEmpty(host.Query)
            || !string.IsNullOrEmpty(host.Fragment)
            || !string.IsNullOrEmpty(host.UserInfo))
        {
            throw new InvalidOperationException("FusionSolar:Host must be an HTTPS host name without a path or credentials.");
        }

        return host;
    }

    private static Uri ResolveRegionalLoginUri(JsonElement root, Uri loginHost)
    {
        string? redirectPath = null;
        if (TryGetProperty(root, "respMultiRegionName", out JsonElement multiRegion)
            && multiRegion.ValueKind == JsonValueKind.Array
            && multiRegion.GetArrayLength() > 1
            && multiRegion[1].ValueKind == JsonValueKind.String)
        {
            redirectPath = multiRegion[1].GetString();
        }

        redirectPath ??= GetOptionalString(root, "redirectURL");
        if (string.IsNullOrWhiteSpace(redirectPath))
            throw new InvalidOperationException("FusionSolar sign-in did not return a regional login URL.");

        Uri redirect = new(loginHost, redirectPath);
        if (redirect.Scheme != Uri.UriSchemeHttps || !string.Equals(redirect.Host, loginHost.Host, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("FusionSolar returned an invalid regional login URL.");

        return redirect;
    }

    private static List<ConfigSignalChange> NormalizeConfigSignals(object payload)
    {
        JsonElement root = JsonSerializer.SerializeToElement(payload, payload.GetType(), JsonOptions);
        JsonElement[] items = root.ValueKind == JsonValueKind.Array ? [.. root.EnumerateArray()] : [root];
        if (items.Length == 0)
            throw new ArgumentException("At least one FusionSolar signal must be supplied.", nameof(payload));

        List<ConfigSignalChange> changes = new(items.Length);
        foreach (JsonElement item in items)
        {
            if (item.ValueKind != JsonValueKind.Object
                || !TryGetProperty(item, "id", out JsonElement id)
                || !TryGetProperty(item, "value", out JsonElement value)
                || !IsValidSignalId(id)
                || !IsValidSignalValue(value))
            {
                throw new ArgumentException("Each FusionSolar signal must contain a string or number id and value.", nameof(payload));
            }

            changes.Add(new ConfigSignalChange(id.Clone(), value.Clone()));
        }

        return changes;
    }

    private static bool SignalIdIs(ConfigSignalChange change, string id)
    {
        return change.Id.ValueKind == JsonValueKind.String
            ? string.Equals(change.Id.GetString(), id, StringComparison.Ordinal)
            : change.Id.ValueKind == JsonValueKind.Number
                && change.Id.TryGetDecimal(out decimal numericId)
                && numericId.ToString(CultureInfo.InvariantCulture) == id;
    }

    private static bool IsValidSignalId(JsonElement id)
    {
        return id.ValueKind == JsonValueKind.String
            ? !string.IsNullOrWhiteSpace(id.GetString())
            : id.ValueKind == JsonValueKind.Number
                && id.TryGetDouble(out double numericId)
                && double.IsFinite(numericId);
    }

    private static bool IsValidSignalValue(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.String
            || value.ValueKind == JsonValueKind.Number
                && value.TryGetDouble(out double numericValue)
                && double.IsFinite(numericValue);
    }

    private static bool TryGetDecimal(JsonElement element, string propertyName, out decimal value)
    {
        value = 0m;
        if (!TryGetProperty(element, propertyName, out JsonElement property))
            return false;

        if (property.ValueKind == JsonValueKind.Number)
            return property.TryGetDecimal(out value);

        if (property.ValueKind == JsonValueKind.String)
        {
            string? text = property.GetString()?.Trim();
            string firstToken = text?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            return decimal.TryParse(firstToken, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        return false;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
            return element.TryGetProperty(name, out value);

        value = default;
        return false;
    }

    private static string BuildQuery(IEnumerable<(string Name, string Value)> values)
    {
        return string.Join('&', values.Select(item =>
            $"{Uri.EscapeDataString(item.Name)}={Uri.EscapeDataString(item.Value)}"));
    }

    private string GetRequiredString(JsonElement element, string name, HttpResponseMessage response)
    {
        return GetRequiredAuthenticationValue(element, name, response, allowNumber: false);
    }

    private string GetRequiredScalarString(JsonElement element, string name, HttpResponseMessage response)
    {
        return GetRequiredAuthenticationValue(element, name, response, allowNumber: true);
    }

    private string GetRequiredAuthenticationValue(
        JsonElement element,
        string name,
        HttpResponseMessage response,
        bool allowNumber)
    {
        if (TryGetAuthenticationProperty(element, name, out JsonElement property))
        {
            if (property.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(property.GetString()))
                return property.GetString()!;

            // The legacy JavaScript client interpolates the timestamp directly into the URL,
            // which also works when FusionSolar encodes it as a JSON number.
            if (allowNumber && property.ValueKind == JsonValueKind.Number)
                return property.GetRawText();
        }

        string fields = DescribeJsonFields(element);
        string? contentType = response.Content.Headers.ContentType?.MediaType;
        logger.LogWarning(
            "FusionSolar.Authentication :: public-key response did not contain a usable {field}; HTTP {statusCode}, content type {contentType}, JSON fields {jsonFields}",
            name,
            (int)response.StatusCode,
            contentType ?? "unknown",
            fields);
        throw new InvalidOperationException(
            $"FusionSolar sign-in response is missing or has an invalid {name} (HTTP {(int)response.StatusCode}; JSON fields: {fields}).");
    }

    private static bool TryGetAuthenticationProperty(JsonElement element, string name, out JsonElement value)
    {
        value = default;
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        if (element.TryGetProperty(name, out value))
            return true;

        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)
                || NormalizeJsonName(property.Name) == NormalizeJsonName(name))
            {
                value = property.Value;
                return true;
            }
        }

        return false;
    }

    private static string NormalizeJsonName(string name)
    {
        return string.Concat(name.Where(char.IsLetterOrDigit)).ToLowerInvariant();
    }

    private static string DescribeJsonFields(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return element.ValueKind.ToString();

        List<string> fields = [.. element.EnumerateObject().Select(property => property.Name).Take(16)];
        if (TryGetProperty(element, "data", out JsonElement data) && data.ValueKind == JsonValueKind.Object)
            fields.AddRange(data.EnumerateObject().Select(property => $"data.{property.Name}").Take(16 - fields.Count));

        return fields.Count == 0 ? "(none)" : string.Join(", ", fields);
    }

    private static string? GetOptionalString(JsonElement element, string name)
    {
        return TryGetProperty(element, name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static StringContent JsonContent<T>(T payload)
    {
        return new(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
    }

    private sealed record ConfigSignalChange(
        [property: JsonPropertyName("id")] JsonElement Id,
        [property: JsonPropertyName("value")] JsonElement Value);

    private sealed class FusionSolarSession(Uri dataHost, string sessionCookie)
    {
        public Uri DataHost { get; } = new(dataHost.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute);
        public string CookieHeader { get; } = $"locale=en-us; dp-session={sessionCookie}";
        public string StationId { get; set; } = string.Empty;
        public string? DongleDn { get; set; }
        public string? BatteryDn { get; set; }
        public string? CsrfToken { get; set; }
        public DateTimeOffset CsrfUpdatedAt { get; set; }
    }

    private sealed class FusionSolarSessionExpiredException : Exception { }
}
