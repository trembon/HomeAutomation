namespace HomeAutomation.Core.Models;

public class IkeaDirigeraEventModel
{
    public string? DeviceId { get; set; }

    public Dictionary<string, string?> Attributes { get; set; } = [];

    public DateTime Timestamp { get; set; }

    public string RawPayload { get; set; } = string.Empty;
}
