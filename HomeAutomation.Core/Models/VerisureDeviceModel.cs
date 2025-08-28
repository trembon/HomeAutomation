namespace HomeAutomation.Core.Models;

public class VerisureDeviceModel
{
    public string Id { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? State { get; set; }
}
