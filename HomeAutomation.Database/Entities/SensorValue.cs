using HomeAutomation.Database.Enums;

namespace HomeAutomation.Database.Entities;

public class SensorValue
{
    public int Id { get; set; }

    public int DeviceId { get; set; }

    public Device Device { get; set; } = null!;

    public SensorValueKind Type { get; set; }

    public string Value { get; set; } = null!;

    public DateTime Timestamp { get; set; }
}
