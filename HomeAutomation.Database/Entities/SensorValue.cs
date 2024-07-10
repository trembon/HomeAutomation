using HomeAutomation.Database.Enums;
using System.ComponentModel.DataAnnotations;

namespace HomeAutomation.Database.Entities;

public class SensorValue
{
    [Key]
    [Required]
    public int ID { get; set; }

    [Required]
    public DeviceSource Source { get; set; }

    [Required]
    public string SourceID { get; set; } = null!;

    [Required]
    public SensorValueType Type { get; set; }

    [Required]
    public string Value { get; set; } = null!;

    [Required]
    public DateTime Timestamp { get; set; }
}
