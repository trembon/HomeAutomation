using HomeAutomation.Core.Models;
using HomeAutomation.Database.Enums;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace HomeAutomation.Core.Services;

public interface IVerisureAPIService
{
    SensorValueKind MapTypeToSensorKind(string type);

    DeviceEvent MapStateToDeviceEvent(string state);

    Task<IEnumerable<VerisureDeviceModel>> GetDevices();
}

public class VerisureAPIService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IVerisureAPIService
{
    public DeviceEvent MapStateToDeviceEvent(string state)
    {
        switch (state.ToLowerInvariant())
        {
            case "open": return DeviceEvent.On;
            case "close": return DeviceEvent.Off;
            case "disarmed": return DeviceEvent.Off;
            case "armed": return DeviceEvent.On;
            case "armed_home": return DeviceEvent.Partial;

            default: return DeviceEvent.Unknown;
        }
    }

    public SensorValueKind MapTypeToSensorKind(string type)
    {
        switch (type.ToLowerInvariant())
        {
            case "temperature": return SensorValueKind.Temperature;
            case "humidity": return SensorValueKind.Humidity;
            default: return SensorValueKind.Unknown;
        }
    }

    public async Task<IEnumerable<VerisureDeviceModel>> GetDevices()
    {
        string baseUrl = configuration["Verisure:APIUrl"] ?? "";
        HttpRequestMessage request = new(HttpMethod.Get, $"{baseUrl}devices/");

        var httpClient = httpClientFactory.CreateClient(nameof(VerisureAPIService));
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IEnumerable<VerisureDeviceModel>>() ?? [];
    }
}
