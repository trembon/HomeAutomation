namespace HomeAutomation.Database.Entities;

public class ActionDeviceEntity
{
    public int Id { get; set; }

    public int ActionId { get; set; }

    public ActionEntity Action { get; set; } = null!;

    public int DeviceId { get; set; }

    public DeviceEntity Device { get; set; } = null!;
}
