namespace HomeAutomation.Webhooks.Models.Verisure;

public class DeviceUpdateModel
{
    public string Id { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? By { get; set; }
}
