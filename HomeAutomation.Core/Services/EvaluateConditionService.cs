using HomeAutomation.Core.Entities;
using HomeAutomation.Entities;
using HomeAutomation.Entities.Conditions;

namespace HomeAutomation.Core.Services;

public interface IEvaluateConditionService
{
    Task<bool> MeetConditions(IEntity source, IEnumerable<Condition> conditions);
}

public class EvaluateConditionService : IEvaluateConditionService
{
    private readonly IServiceProvider serviceProvider;

    public EvaluateConditionService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task<bool> MeetConditions(IEntity source, IEnumerable<Condition> conditions)
    {
        // if there are no conditions to match, conditions are "meet"
        if (conditions == null || conditions.Count() == 0)
            return true;

        List<bool> results = new(conditions.Count());
        foreach (var condition in conditions)
        {
            var arguments = new ConditionExecutionArguments(source, serviceProvider);
            bool result = await condition.Check(arguments);
            results.Add(result);
        }

        return results.All(x => x);
    }
}
