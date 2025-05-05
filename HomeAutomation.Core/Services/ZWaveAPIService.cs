using HomeAutomation.Base.Enums;
using HomeAutomation.Core.Models;
using HomeAutomation.Database.Enums;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

namespace HomeAutomation.Core.Services;

public interface IZWaveAPIService
{
    event Action<ZWaveEventModel> ZWaveEventReceived;

    Task<IEnumerable<ZWaveDeviceModel>> GetNodes();

    Task<bool> SendCommand(byte id, ZWaveCommandClass command, object value);

    void SendEventMessage(string message);

    void SendEventMessage(string message, DateTime timestamp);

    ZWaveCommandClass? ConvertStateToCommand(DeviceEvent state, out object value);

    DeviceEvent ConvertParameterToEvent(DeviceKind deviceKind, ZWaveEventParameter? parameter, object? value);
}

public class ZWaveAPIService : IZWaveAPIService
{
    private readonly HttpClient httpClient;
    private readonly IConfiguration configuration;

    public event Action<ZWaveEventModel> ZWaveEventReceived;

    public ZWaveAPIService(IConfiguration configuration)
    {
        this.httpClient = new HttpClient();
        this.configuration = configuration;
    }

    public async Task<IEnumerable<ZWaveDeviceModel>> GetNodes()
    {
        string baseUrl = configuration.GetSection("ZWave:APIUrl").Get<string>();
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}nodes/");

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IEnumerable<ZWaveDeviceModel>>();
    }

    public async Task<bool> SendCommand(byte id, ZWaveCommandClass command, object parameter)
    {
        string baseUrl = configuration.GetSection("ZWave:APIUrl").Get<string>();
        HttpRequestMessage request = new(HttpMethod.Post, $"{baseUrl}nodes/{id}/send/{command}?parameter={parameter}");

        var sendResult = await httpClient.SendAsync(request);
        sendResult.EnsureSuccessStatusCode();

        return true; // TODO: check for real response
    }

    public void SendEventMessage(string message)
    {
        SendEventMessage(message, DateTime.Now);
    }

    public void SendEventMessage(string message, DateTime timestamp)
    {
        ZWaveEventReceived?.Invoke(new ZWaveEventModel { Message = message, Timestamp = timestamp });
    }

    public ZWaveCommandClass? ConvertStateToCommand(DeviceEvent state, out object value)
    {
        switch (state)
        {
            case DeviceEvent.On:
                value = 255;
                return ZWaveCommandClass.Basic;

            case DeviceEvent.Off:
                value = 0;
                return ZWaveCommandClass.Basic;
        }

        value = null;
        return null;
    }

    public DeviceEvent ConvertParameterToEvent(DeviceKind deviceKind, ZWaveEventParameter? parameter, object? value)
    {
        if (value is JsonElement je)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.String:
                    value = je.GetString();
                    break;

                case JsonValueKind.Number:
                    if (je.TryGetInt32(out int intValue))
                    {
                        value = intValue;
                    }
                    else if (je.TryGetDecimal(out decimal decimalValue))
                    {
                        value = decimalValue;
                    }
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    value = je.GetBoolean();
                    break;

                default:
                    value = null;
                    break;
            }
        }

        if (deviceKind == DeviceKind.MotionSensor)
        {
            if (parameter == ZWaveEventParameter.SensorMotion)
                return DeviceEvent.Motion;

            if (parameter == ZWaveEventParameter.AlarmGeneric)
                return DeviceEvent.Off;
        }

        switch (value)
        {
            case int intValue:
                if (parameter == ZWaveEventParameter.Basic)
                    return intValue == 255 ? DeviceEvent.On : DeviceEvent.Off;

                if (parameter == ZWaveEventParameter.SwitchBinary)
                    return intValue == 255 ? DeviceEvent.On : DeviceEvent.Off;

                break;

            case byte byteValue:
                if (parameter == ZWaveEventParameter.Basic)
                    return byteValue == 255 ? DeviceEvent.On : DeviceEvent.Off;

                if (parameter == ZWaveEventParameter.SwitchBinary)
                    return byteValue == 255 ? DeviceEvent.On : DeviceEvent.Off;

                break;
        }

        return DeviceEvent.Unknown;
    }
}
