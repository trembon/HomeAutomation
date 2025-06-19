using HomeAutomation.Base.Enums;

namespace HomeAutomation.Webhooks.Models.ZWave;

public class NodeUpdateModel
{
    public byte NodeId { get; set; }

    public DateTime Timestamp { get; set; }

    public ZWaveEventParameter ValueType { get; set; }

    public object? Value { get; set; }
}
