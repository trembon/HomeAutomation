using HomeAutomation.Database.Enums;

namespace HomeAutomation.Database.Entities;

public class Condition
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public ConditionKind Kind { get; set; }

    public TimeMode? TimeMode { get; set; }

    public TimeOnly? TimeSpecified { get; set; }

    public CompareKind TimeCompareKind { get; set; }
}
