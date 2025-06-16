using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Interfaces;

namespace HomeAutomation.Database.Entities;

public class TriggerEntity : BaseEntity, IConditionedEntity
{
    public string Name { get; set; } = null!;

    public TriggerKind Kind { get; set; }

    public bool Disabled { get; set; } = false;

    public DeviceEvent? ListenOnDeviceEvent { get; set; }

    public int? ListenOnDeviceId { get; set; }

    public DeviceEntity? ListenOnDevice { get; set; }

    public TimeMode? SchedulingMode { get; set; }

    public TimeOnly? ScheduledAt { get; set; }

    public List<TriggerActionEntity> Actions { get; set; } = [];

    public List<ConditionEntity> Conditions { get; set; } = [];
}
