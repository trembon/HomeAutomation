namespace HomeAutomation.Core.Models;

public sealed class FusionSolarEnergyFlowModel
{
    public string StationId { get; init; } = string.Empty;

    public DateTimeOffset Timestamp { get; init; }

    public decimal GridFlow { get; init; }

    public decimal BatteryFlow { get; init; }

    public decimal BatteryChargeLevel { get; init; }

    public decimal HouseConsumption { get; init; }

    public decimal SolarGeneration { get; init; }
}
