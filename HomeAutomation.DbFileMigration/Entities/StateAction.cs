using HomeAutomation.Core.Entities;
using HomeAutomation.Entities.Enums;

namespace HomeAutomation.Entities.Action;

public class StateAction : Action
{
    public DeviceState State { get; set; }

    public Dictionary<string, string> Parameters { get; set; }

    public override Task Execute(IActionExecutionArguments arguments)
    {
        return Task.CompletedTask;
    }
}
