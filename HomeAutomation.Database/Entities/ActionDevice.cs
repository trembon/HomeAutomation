namespace HomeAutomation.Database.Entities;

public class ActionDevice
{
    public int Id { get; set; }

    public int ActionId { get; set; }

    public Action Action { get; set; } = null!;

    public int DeviceId { get; set; }

    public Device Device { get; set; } = null!;
}
