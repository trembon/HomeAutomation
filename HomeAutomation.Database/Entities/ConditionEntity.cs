using HomeAutomation.Database.Enums;

namespace HomeAutomation.Database.Entities;

public class ConditionEntity : BaseEntity
{
    public string Name { get; set; } = null!;

    public ConditionKind Kind { get; set; }

    public TimeMode? TimeMode { get; set; }

    public TimeOnly? TimeSpecified { get; set; }

    public CompareKind TimeCompareKind { get; set; }

    public List<ActionEntity> Actions { get; set; } = [];

    public List<TriggerEntity> Triggers { get; set; } = [];
}
