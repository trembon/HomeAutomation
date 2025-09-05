using HomeAutomation.Core.Models;
using HomeAutomation.Database.Enums;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace HomeAutomation.Core.Services;

public interface IFusionSolarService
{
    SensorValueKind MapTypeToSensorKind(string property);
    Task<IEnumerable<FusionSolarDeviceModel>> GetDevices();
}

public class FusionSolarService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IFusionSolarService
{
    public SensorValueKind MapTypeToSensorKind(string property)
    {
        switch (property.ToLowerInvariant())
        {
            case "gridFlow": return SensorValueKind.EnergyFlow;
            case "batteryFlow": return SensorValueKind.BatteryFlow;
            case "batteryChargeLevel": return SensorValueKind.BatteryChargeLevel;
            case "houseConsumption": return SensorValueKind.ConsumptionFlow;
            case "solarGeneration": return SensorValueKind.EnergyGeneration;
            default: return SensorValueKind.Unknown;
        }
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
}
