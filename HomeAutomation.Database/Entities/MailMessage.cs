using System.ComponentModel.DataAnnotations;

namespace HomeAutomation.Database;

public class MailMessage
{
    [Key]
    [Required]
    public int ID { get; set; }

    public string MessageID { get; set; } = null!;

    public string? DeviceSource { get; set; }

    public string? DeviceSourceID { get; set; }

    public byte[] EmlData { get; set; } = null!;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
