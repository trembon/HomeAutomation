namespace HomeAutomation.Webhooks.Models.Tuya;

public class DeviceUpdateModel
{
    public string DeviceId { get; set; }

    public Dictionary<int, object> Data { get; set; }
}
