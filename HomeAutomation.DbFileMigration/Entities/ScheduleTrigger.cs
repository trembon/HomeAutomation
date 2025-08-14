using HomeAutomation.Entities.Enums;

namespace HomeAutomation.Entities.Triggers;

public class ScheduleTrigger : Trigger
{
    public TimeSpan At { get; set; }

    public ScheduleMode Mode { get; set; }

    public override string ToSourceString()
    {
        return "schedulering";
    }
}
