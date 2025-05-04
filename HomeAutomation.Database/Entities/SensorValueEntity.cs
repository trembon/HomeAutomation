using HomeAutomation.Database.Enums;

namespace HomeAutomation.Database.Entities;

public class SensorValueEntity : BaseEntity
{
    public int DeviceId { get; set; }

    public DeviceEntity Device { get; set; } = null!;

    public SensorValueKind Type { get; set; }

    public string Value { get; set; } = null!;

    public DateTime Timestamp { get; set; }
}
