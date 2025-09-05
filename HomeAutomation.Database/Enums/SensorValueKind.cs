namespace HomeAutomation.Database.Enums;

[Flags]
public enum SensorValueKind : int
{
    Unknown = 0,
    Temperature = 1,
    Humidity = 2,
    RainRate = 4,
    RainTotal = 8,
    WindDirection = 16,
    WindAverage = 32,
    WindGust = 64,
    EnergyFlow = 128,
    BatteryFlow = 256,
    BatteryChargeLevel = 512,
    ConsumptionFlow = 1024,
    EnergyGeneration = 2048,
}
