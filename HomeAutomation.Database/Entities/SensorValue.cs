using HomeAutomation.Database.Enums;
using System.ComponentModel.DataAnnotations;

namespace HomeAutomation.Database.Entities;

public class SensorValue
{
    [Key]
    [Required]
    public int ID { get; set; }
    
    [Required]
    public int TellstickID { get; set; }

    [Required]
    public SensorValueType Type { get; set; }

    public string Value { get; set; } = null!;

    public DateTime Timestamp { get; set; }
}
