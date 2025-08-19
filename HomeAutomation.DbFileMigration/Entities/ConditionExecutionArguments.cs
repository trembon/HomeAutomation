using HomeAutomation.Entities;

namespace HomeAutomation.Core.Entities;

public class ConditionExecutionArguments
{
    public IEntity Source { get; set; }

    public ConditionExecutionArguments(IEntity source)
    {
        this.Source = source;
    }
}
