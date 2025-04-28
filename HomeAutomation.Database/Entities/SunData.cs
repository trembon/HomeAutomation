namespace HomeAutomation.Database.Entities;

public class SunData
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly Sunset { get; set; }

    public TimeOnly Sunrise { get; set; }
}
