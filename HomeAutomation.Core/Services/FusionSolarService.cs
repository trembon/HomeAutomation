using HomeAutomation.Core.Extensions;
using HomeAutomation.Core.Models;
using HomeAutomation.Database.Enums;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeAutomation.Core.Services;

public interface IFusionSolarService
{
    SensorValueKind MapTypeToSensorKind(string property);
    Task<IEnumerable<FusionSolarDeviceModel>> GetDevices(CancellationToken cancellationToken = default);
    Task<bool> SetConfigSignals(string type, object payload, CancellationToken cancellationToken);
}

public class FusionSolarService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IFusionSolarService
{
    public SensorValueKind MapTypeToSensorKind(string property)
    {
        return property switch
        {
            "gridFlow" => SensorValueKind.EnergyFlow,
            "batteryFlow" => SensorValueKind.EnergyFlow,
            "batteryChargeLevel" => SensorValueKind.ChargeLevel,
            "houseConsumption" => SensorValueKind.EnergyFlow,
            "solarGeneration" => SensorValueKind.EnergyFlow,
            _ => SensorValueKind.Unknown,
        };
    }

    public async Task<IEnumerable<FusionSolarDeviceModel>> GetDevices(CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(configuration.GetRequiredHttpBaseUri("FusionSolar:APIUrl"), "devices/");
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        HttpClient httpClient = httpClientFactory.CreateClient(nameof(FusionSolarService));
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength == 0)
            return [];

        return await response.Content.ReadFromJsonAsync<IEnumerable<FusionSolarDeviceModel>>(cancellationToken) ?? [];
    }

    public async Task<bool> SetConfigSignals(string type, object payload, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(payload);
        using StringContent content = new(json, Encoding.UTF8, "application/json");
        string escapedType = Uri.EscapeDataString(type);
        Uri requestUri = new(configuration.GetRequiredHttpBaseUri("FusionSolar:APIUrl"), $"set-config-signals?type={escapedType}");
        HttpClient httpClient = httpClientFactory.CreateClient(nameof(FusionSolarService));
        using HttpResponseMessage response = await httpClient.PostAsync(requestUri, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        SetConfigSignalsResponse? result = await response.Content.ReadFromJsonAsync<SetConfigSignalsResponse>(cancellationToken);
        return result?.Result ?? false;
    }

    private sealed class SetConfigSignalsResponse
    {
        [JsonPropertyName("result")]
        public bool Result { get; set; }
    }
}
