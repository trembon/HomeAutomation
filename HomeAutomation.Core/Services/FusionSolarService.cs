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
    Task<IEnumerable<FusionSolarDeviceModel>> GetDevices();
    Task<bool> SetConfigSignals(object payload, CancellationToken cancellationToken);
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

    public async Task<IEnumerable<FusionSolarDeviceModel>> GetDevices()
    {
        string baseUrl = configuration["FusionSolar:APIUrl"] ?? "";
        HttpRequestMessage request = new(HttpMethod.Get, $"{baseUrl}devices/");

        var httpClient = httpClientFactory.CreateClient(nameof(FusionSolarService));
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IEnumerable<FusionSolarDeviceModel>>() ?? [];
    }

    public async Task<bool> SetConfigSignals(object payload, CancellationToken cancellationToken)
    {
        string baseUrl = configuration["FusionSolar:APIUrl"] ?? "";

        string json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpClient = httpClientFactory.CreateClient(nameof(FusionSolarService));
        HttpResponseMessage response = await httpClient.PostAsync($"{baseUrl}set-config-signals", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SetConfigSignalsResponse>();
        return result?.Result ?? false;
    }

    private class SetConfigSignalsResponse
    {
        [JsonPropertyName("result")]
        public bool Result { get; set; }
    }
}
