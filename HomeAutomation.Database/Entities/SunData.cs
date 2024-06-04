using System.ComponentModel.DataAnnotations;

namespace HomeAutomation.Database.Entities;

public class SunData
{
    [Key]
    [Required]
    public int ID { get; set; }
    
    public DateTime Date { get; set; }
    
    public DateTime Sunset { get; set; }

    public DateTime Sunrise { get; set; }
}
