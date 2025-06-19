namespace HomeAutomation.Webhooks.Models.Tuya;

public class DeviceUpdateModel
{
    public string DeviceId { get; set; } = null!;

    public Dictionary<int, object> Data { get; set; } = [];
}
