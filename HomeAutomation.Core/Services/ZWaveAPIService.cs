using HomeAutomation.Core.Enums;
using HomeAutomation.Core.Extensions;
using HomeAutomation.Core.Models;
using HomeAutomation.Database.Enums;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

namespace HomeAutomation.Core.Services;

public interface IZWaveAPIService
{
    event Action<ZWaveEventModel>? ZWaveEventReceived;

    Task<IEnumerable<ZWaveDeviceModel>> GetNodes(CancellationToken cancellationToken = default);

    Task<bool> SendCommand(byte id, ZWaveCommandClass command, object parameter, CancellationToken cancellationToken = default);

    void SendEventMessage(string message);

    void SendEventMessage(string message, DateTime timestamp);

    ZWaveCommandClass? ConvertStateToCommand(DeviceEvent state, out object? value);

    DeviceEvent ConvertParameterToEvent(DeviceKind deviceKind, ZWaveEventParameter? parameter, object? value);
}

public class ZWaveAPIService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IZWaveAPIService
{
    public event Action<ZWaveEventModel>? ZWaveEventReceived;

    public async Task<IEnumerable<ZWaveDeviceModel>> GetNodes(CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(configuration.GetRequiredHttpBaseUri("ZWave:APIUrl"), "nodes/");
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        HttpClient httpClient = httpClientFactory.CreateClient(nameof(ZWaveAPIService));
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength == 0)
            return [];

        return await response.Content.ReadFromJsonAsync<IEnumerable<ZWaveDeviceModel>>(cancellationToken) ?? [];
    }

    public async Task<bool> SendCommand(byte id, ZWaveCommandClass command, object parameter, CancellationToken cancellationToken = default)
    {
        string escapedParameter = Uri.EscapeDataString(Convert.ToString(parameter, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
        Uri requestUri = new(configuration.GetRequiredHttpBaseUri("ZWave:APIUrl"), $"nodes/{id}/send/{command}?parameter={escapedParameter}");
        using HttpRequestMessage request = new(HttpMethod.Post, requestUri);
        HttpClient httpClient = httpClientFactory.CreateClient(nameof(ZWaveAPIService));
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return true;
    }

    public void SendEventMessage(string message)
    {
        SendEventMessage(message, DateTime.Now);
    }

    public void SendEventMessage(string message, DateTime timestamp)
    {
        ZWaveEventReceived?.Invoke(new ZWaveEventModel { Message = message, Timestamp = timestamp });
    }

    public ZWaveCommandClass? ConvertStateToCommand(DeviceEvent state, out object? value)
    {
        switch (state)
        {
            case DeviceEvent.On:
                value = 255;
                return ZWaveCommandClass.Basic;

            case DeviceEvent.Off:
                value = 0;
                return ZWaveCommandClass.Basic;

            default:
                value = null;
                return null;
        }
    }

    public DeviceEvent ConvertParameterToEvent(DeviceKind deviceKind, ZWaveEventParameter? parameter, object? value)
    {
        if (value is JsonElement jsonElement)
        {
            value = jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number when jsonElement.TryGetInt32(out int integerValue) => integerValue,
                JsonValueKind.Number when jsonElement.TryGetDecimal(out decimal decimalValue) => decimalValue,
                JsonValueKind.True or JsonValueKind.False => jsonElement.GetBoolean(),
                _ => null
            };
        }

        if (deviceKind == DeviceKind.MotionSensor)
        {
            if (parameter == ZWaveEventParameter.SensorMotion)
                return DeviceEvent.Motion;

            if (parameter == ZWaveEventParameter.AlarmGeneric)
                return DeviceEvent.Off;
        }

        return value switch
        {
            int integerValue when parameter is ZWaveEventParameter.Basic or ZWaveEventParameter.SwitchBinary
                => integerValue == 255 ? DeviceEvent.On : DeviceEvent.Off,
            byte byteValue when parameter is ZWaveEventParameter.Basic or ZWaveEventParameter.SwitchBinary
                => byteValue == 255 ? DeviceEvent.On : DeviceEvent.Off,
            _ => DeviceEvent.Unknown
        };
    }
}
