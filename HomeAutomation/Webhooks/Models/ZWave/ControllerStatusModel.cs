using HomeAutomation.Base.Enums;

namespace HomeAutomation.Webhooks.Models.ZWave;

public class ControllerStatusModel
{
    public DateTime Timestamp { get; set; }

    public ZWaveControllerStatus Status { get; set; }
}
