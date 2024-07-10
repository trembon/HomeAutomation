using HomeAutomation.Base.Enums;

namespace HomeAutomation.Webhooks.Models.ZWave;

public class DiscoveryProgressModel
{
    public DateTime Timestamp { get; set; }

    public ZWaveDiscoveryStatus Status { get; set; }
}
