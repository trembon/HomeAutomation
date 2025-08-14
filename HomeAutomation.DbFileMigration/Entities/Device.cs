using HomeAutomation.Database.Enums;

namespace HomeAutomation.Entities.Devices;

public abstract class Device : IEntity
{
    public int ID { get; set; }

    public string Name { get; set; }

    public DeviceSource Source { get; set; }

    public string SourceID { get; set; }

    public string UniqueID => $"{nameof(Device)}_{ID}";

    public override string ToString()
    {
        return $"{Name} (ID: {ID})";
    }

    public virtual string ToSourceString()
    {
        return $"länkning ({Name})";
    }
}
