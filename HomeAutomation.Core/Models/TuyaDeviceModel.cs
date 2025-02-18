namespace HomeAutomation.Core.Models;

public class TuyaDeviceModel
{
    public string ID { get; set; }

    public string Name { get; set; }

    public string IP { get; set; }

    public string ProductKey { get; set; }

    public bool IsConnected { get; set; }

    public override int GetHashCode()
    {
        return this.ID.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (obj != null && obj is TuyaDeviceModel model)
            return model.ID == this.ID;

        return false;
    }
}
