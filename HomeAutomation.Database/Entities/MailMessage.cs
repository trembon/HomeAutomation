namespace HomeAutomation.Database.Entities;

public class MailMessage
{
    public int Id { get; set; }

    public int? DeviceId { get; set; }

    public string MessageId { get; set; } = null!;

    public byte[] EmlData { get; set; } = null!;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Device? Device { get; set; }
}
