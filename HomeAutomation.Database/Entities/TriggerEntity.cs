using HomeAutomation.Database.Enums;

namespace HomeAutomation.Database.Entities;

public class TriggerEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public TriggerKind Kind { get; set; }

    public bool Disabled { get; set; } = false;

    public int? ConditionId { get; set; }

    public ConditionEntity? Condition { get; set; }

    public DeviceEvent? ListenOnDeviceEvent { get; set; }

    public int? ListenOnDeviceId { get; set; }

    public DeviceEntity? ListenOnDevice { get; set; }

    public TimeMode? SchedulingMode { get; set; }

    public TimeOnly? ScheduledAt { get; set; }

    public List<TriggerActionEntity> Actions { get; set; } = [];
}
