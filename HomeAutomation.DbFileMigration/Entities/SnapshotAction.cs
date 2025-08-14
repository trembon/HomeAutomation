using HomeAutomation.Core.Entities;

namespace HomeAutomation.Entities.Action;

public class SnapshotAction : MessageAction
{
    public override Task Execute(IActionExecutionArguments arguments)
    {
        return Task.CompletedTask;
    }
}
