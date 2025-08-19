using HomeAutomation.Core.Entities;
using HomeAutomation.Entities.Enums;

namespace HomeAutomation.Entities.Conditions;

public class TimeCondition : Condition
{
    public ScheduleMode Mode { get; set; }

    public TimeSpan? Time { get; set; }

    public CompareType Compare { get; set; }

    public override Task<bool> Check(ConditionExecutionArguments arguments)
    {

        return Task.FromResult(false);
    }
}
