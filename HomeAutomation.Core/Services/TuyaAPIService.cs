using HomeAutomation.Core.Extensions;
using HomeAutomation.Core.Models;
using HomeAutomation.Database.Enums;
using Microsoft.Extensions.Configuration;
using System.Drawing;
using System.Net.Http.Json;

namespace HomeAutomation.Core.Services;

public interface ITuyaAPIService
{
    Task<IEnumerable<TuyaDeviceModel>> GetDevices(CancellationToken cancellationToken = default);

    Task<bool> SendCommand(string deviceId, Dictionary<int, object> dps, CancellationToken cancellationToken = default);

    Dictionary<int, object> ConvertStateToDPS(DeviceEvent state, DeviceKind deviceKind, Dictionary<string, string> parameters);

    DeviceEvent ConvertPropertyToEvent(DeviceKind deviceKind, Dictionary<int, object>? dps);
}

public class TuyaAPIService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : ITuyaAPIService
{
    public DeviceEvent ConvertPropertyToEvent(DeviceKind deviceKind, Dictionary<int, object>? dps)
    {
        if (dps is null)
            return DeviceEvent.Unknown;

        switch (deviceKind)
        {
            case DeviceKind.Lightbulb:
                if (dps.ContainsKey(20))
                {
                    return Convert.ToBoolean(dps[20]) ? DeviceEvent.On : DeviceEvent.Off;
                }
                break;

            case DeviceKind.PowerSwitch:
                if (dps.ContainsKey(1))
                {
                    return Convert.ToBoolean(dps[1]) ? DeviceEvent.On : DeviceEvent.Off;
                }
                break;
        }

        return DeviceEvent.Unknown;
    }

    public Dictionary<int, object> ConvertStateToDPS(DeviceEvent state, DeviceKind deviceKind, Dictionary<string, string> parameters)
    {
        Dictionary<int, object> result = [];

        switch (deviceKind)
        {
            case DeviceKind.Lightbulb:
                if (state == DeviceEvent.On)
                {
                    result[20] = true;

                    // color: HSV value of the wanted color in hex values
                    // hue 0 - 360, saturation 0 - 360, value 0 - 1000
                    // ex: 00DC004B004E > 00DC|004B|004E > hue|saturation|value
                    if (parameters.TryGetValue("color", out string? hexColor) && hexColor is not null)
                    {
                        result[21] = "colour";
                        result[24] = ColorTranslator.FromHtml(hexColor).ToHSVString();
                    }
                    else
                    {
                        result[21] = "white";

                        // brightness and temperature is only applicable for white light

                        // brightness: 10-1000 (1-100%)
                        // if not specified, always go for 100%
                        if (parameters.TryGetValue("brightness", out string? brightness) && brightness is not null)
                        {
                            result[22] = int.Parse(brightness) * 10;
                        }
                        else
                        {
                            result[22] = 1000;
                        }

                        // temperature: 0-1000 (0-100%)
                        // if not specified, always go for 100%
                        if (parameters.TryGetValue("temperature", out string? temperature) && temperature is not null)
                        {
                            result[23] = int.Parse(temperature) * 10;
                        }
                        else
                        {
                            result[23] = 1000;
                        }
                    }
                }
                if (state == DeviceEvent.Off)
                {
                    result[20] = false;
                    result[21] = "white";
                }
                break;

            case DeviceKind.PowerSwitch:
                if (state == DeviceEvent.On)
                {
                    result[1] = true;
                }
                if (state == DeviceEvent.Off)
                {
                    result[1] = false;
                }
                break;
        }

        return result;
    }

    public async Task<IEnumerable<TuyaDeviceModel>> GetDevices(CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(configuration.GetRequiredHttpBaseUri("Tuya:APIUrl"), "devices/");
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        HttpClient httpClient = httpClientFactory.CreateClient(nameof(TuyaAPIService));
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength == 0)
            return [];

        return await response.Content.ReadFromJsonAsync<IEnumerable<TuyaDeviceModel>>(cancellationToken) ?? [];
    }

    public async Task<bool> SendCommand(string deviceId, Dictionary<int, object> dps, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        Uri requestUri = new(configuration.GetRequiredHttpBaseUri("Tuya:APIUrl"), $"devices/{Uri.EscapeDataString(deviceId)}/send");
        using HttpRequestMessage request = new(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(new { data = dps })
        };

        HttpClient httpClient = httpClientFactory.CreateClient(nameof(TuyaAPIService));
        using HttpResponseMessage sendResult = await httpClient.SendAsync(request, cancellationToken);
        sendResult.EnsureSuccessStatusCode();

        return true; // TODO: check for real response
    }
}
