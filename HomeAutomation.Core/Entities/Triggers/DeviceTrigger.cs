using HomeAutomation.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Entities.Triggers;

public class DeviceTrigger : Trigger
{
    public DeviceEvent[] Events { get; set; }

    public int[] Devices { get; set; }

    public override string ToSourceString()
    {
        return "länkning";
    }
}
