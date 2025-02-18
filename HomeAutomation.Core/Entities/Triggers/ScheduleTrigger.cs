using HomeAutomation.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
