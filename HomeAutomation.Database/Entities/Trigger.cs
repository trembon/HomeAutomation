using HomeAutomation.Database.Enums;

namespace HomeAutomation.Database.Entities;

public class Trigger
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public TriggerKind Kind { get; set; }

    public bool Disabled { get; set; } = false;

    public int? ConditionId { get; set; }

    public Condition? Condition { get; set; }

    public DeviceEvent? ListenOnDeviceEvent { get; set; }

    public int? ListenOnDeviceId { get; set; }

    public Device? ListenOnDevice { get; set; }

    public TimeMode? SchedulingMode { get; set; }

    public TimeOnly? ScheduledAt { get; set; }

    public List<TriggerAction> Actions { get; set; } = [];
}
