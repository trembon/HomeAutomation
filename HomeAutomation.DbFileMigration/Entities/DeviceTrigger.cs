using HomeAutomation.Entities.Enums;

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
