using HomeAutomation.Database.Enums;

namespace HomeAutomation.Database.Entities;

public class Device
{
    public int Id { get; set; }

    public DeviceSource Source { get; set; }

    public string SourceId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DeviceKind Kind { get; set; }

    public string? Url { get; set; }

    public string? ThumbnailUrl { get; set; }

    public List<Trigger> StateTriggers { get; set; } = [];

    public List<SensorValue> SensorValues { get; set; } = [];

    public List<ActionDevice> Actions { get; set; } = [];

    public override string ToString()
    {
        return $"{Name} (ID: {Id})";
    }
}
