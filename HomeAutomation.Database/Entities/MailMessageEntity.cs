namespace HomeAutomation.Database.Entities;

public class MailMessageEntity : BaseEntity
{
    public int? DeviceId { get; set; }

    public DeviceEntity? Device { get; set; }

    public string MessageId { get; set; } = null!;

    public byte[] EmlData { get; set; } = null!;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
