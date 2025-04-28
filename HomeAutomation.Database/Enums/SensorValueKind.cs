namespace HomeAutomation.Database.Enums;

[Flags]
public enum SensorValueKind : int
{
    Temperature = 1,
    Humidity = 2,
    RainRate = 4,
    RainTotal = 8,
    WindDirection = 16,
    WindAverage = 32,
    WindGust = 64
}
