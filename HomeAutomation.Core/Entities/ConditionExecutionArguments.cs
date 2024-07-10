using HomeAutomation.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace HomeAutomation.Core.Entities;

public class ConditionExecutionArguments
{
    private IServiceProvider serviceProvider;

    public IEntity Source { get; set; }

    public ConditionExecutionArguments(IEntity source, IServiceProvider serviceProvider)
    {
        this.Source = source;
        this.serviceProvider = serviceProvider;
    }

    public T GetService<T>() where T : notnull
    {
        return serviceProvider.GetRequiredService<T>();
    }
}
