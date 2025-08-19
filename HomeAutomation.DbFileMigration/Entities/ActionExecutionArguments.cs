using HomeAutomation.Entities;
using HomeAutomation.Entities.Devices;

namespace HomeAutomation.Core.Entities;

public interface IActionExecutionArguments
{
    IEnumerable<Device> Devices { get; }

    IEntity Source { get; }
}

public class ActionExecutionArguments(IEntity source, IEnumerable<Device> devices) : IActionExecutionArguments
{
    public IEnumerable<Device> Devices { get; } = devices;

    public IEntity Source { get; } = source;
}
