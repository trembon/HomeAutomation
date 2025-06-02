using HomeAutomation.Core.Enums;

namespace HomeAutomation.Core.Models;

public class TelldusDeviceModel
{
    public int ID { get; set; }

    public string Name { get; set; } = null!;

    public string Model { get; set; } = null!;

    public string Protocol { get; set; } = null!;

    public Dictionary<string, string> Parameters { get; set; } = null!;

    public TelldusDeviceMethods SupportedMethods { get; set; }

    public override int GetHashCode()
    {
        return this.ID.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj != null && obj is TelldusDeviceModel model)
            return model.ID == this.ID;

        return false;
    }
}
