using HomeAutomation.Database.Enums;

namespace HomeAutomation.Database.Entities;

public class Action
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public ActionKind Kind { get; set; }

    public bool Disabled { get; set; } = false;

    public DeviceEvent? DeviceEventToSend { get; set; }

    public string? MessageChannel { get; set; }

    public string? MessageToSend { get; set; }

    public List<ActionDevice> Devices { get; set; } = [];

    public List<TriggerAction> Triggers { get; set; } = [];
}
