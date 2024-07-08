using HomeAutomation.Base.Enums;

namespace HomeAutomation.Webhooks.Models.ZWave;

public class HealProgressModel
{
    public DateTime Timestamp { get; set; }

    public ZWaveHealStatus Status { get; set; }
}
