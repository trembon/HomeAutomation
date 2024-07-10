using System.ComponentModel.DataAnnotations;

namespace HomeAutomation.Database.Entities;

public class SunData
{
    [Key]
    [Required]
    public int ID { get; set; }
    
    public DateOnly Date { get; set; }
    
    public TimeOnly Sunset { get; set; }

    public TimeOnly Sunrise { get; set; }
}
