using HomeAutomation.Entities;
using HomeAutomation.Entities.Devices;
using Microsoft.Extensions.DependencyInjection;

namespace HomeAutomation.Core.Entities;

public interface IActionExecutionArguments
{
    IEnumerable<Device> Devices { get; }

    IEntity Source { get; }

    T GetService<T>() where T : notnull;
}

public class ActionExecutionArguments(IEntity source, IEnumerable<Device> devices, IServiceProvider serviceProvider) : IActionExecutionArguments
{
    public IEnumerable<Device> Devices { get; } = devices;

    public IEntity Source { get; } = source;

    public T GetService<T>() where T : notnull
    {
        return serviceProvider.GetRequiredService<T>();
    }
}
