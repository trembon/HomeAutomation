namespace HomeAutomation.Webhooks.Models.FusionSolar;

public class SensorUpdateModel
{
    public string Id { get; set; } = null!;
    public decimal Value { get; set; }
    public string Property { get; set; } = null!;
    public DateTime Timestamp { get; set; }
}
