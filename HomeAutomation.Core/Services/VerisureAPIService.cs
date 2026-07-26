using HomeAutomation.Core.Extensions;
using HomeAutomation.Core.Models;
using HomeAutomation.Database.Enums;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace HomeAutomation.Core.Services;

public interface IVerisureAPIService
{
    SensorValueKind MapTypeToSensorKind(string type);

    DeviceEvent MapStateToDeviceEvent(string state);

    Task<IEnumerable<VerisureDeviceModel>> GetDevices(CancellationToken cancellationToken = default);
}

public class VerisureAPIService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IVerisureAPIService
{
    public DeviceEvent MapStateToDeviceEvent(string state)
    {
        return state.ToLowerInvariant() switch
        {
            "open" => DeviceEvent.On,
            "close" or "disarmed" => DeviceEvent.Off,
            "armed" or "armed_away" => DeviceEvent.On,
            "armed_home" => DeviceEvent.Partial,
            _ => DeviceEvent.Unknown
        };
    }

    public SensorValueKind MapTypeToSensorKind(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "temperature" => SensorValueKind.Temperature,
            "humidity" => SensorValueKind.Humidity,
            _ => SensorValueKind.Unknown
        };
    }

    public async Task<IEnumerable<VerisureDeviceModel>> GetDevices(CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(configuration.GetRequiredHttpBaseUri("Verisure:APIUrl"), "devices/");
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        HttpClient httpClient = httpClientFactory.CreateClient(nameof(VerisureAPIService));
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength == 0)
            return [];

        return await response.Content.ReadFromJsonAsync<IEnumerable<VerisureDeviceModel>>(cancellationToken) ?? [];
    }
}
