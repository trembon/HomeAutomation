namespace HomeAutomation.Webhooks.Models.Verisure;

public class SensorUpdateModel
{
    public string Id { get; set; } = null!;
    public decimal Value { get; set; }
    public string Type { get; set; } = null!;
    public DateTime Timestamp { get; set; }
}
