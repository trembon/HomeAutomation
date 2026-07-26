using HomeAutomation.Core.Enums;

namespace HomeAutomation.Webhooks.Models.ZWave;

public class NodeOperationProgressModel
{
    public byte NodeId { get; set; }

    public DateTime Timestamp { get; set; }

    public ZWaveNodeQueryStatus Status { get; set; }
}
