namespace HomeAutomation.Core.Models;

public class IkeaDirigeraDeviceModel
{
    public string Id { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool IsReachable { get; set; }
}
