using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Interfaces;
using HomeAutomation.Database.Repositories;

namespace HomeAutomation.Core.Services;

public interface IEvaluateConditionService
{
    bool MeetConditions(IConditionedEntity conditionedEntity);

    bool MeetCondition(ConditionEntity? condition);
}

public class EvaluateConditionService(IRepository<ConditionEntity> repository, ISunDataService sunDataService) : IEvaluateConditionService
{
    public bool MeetConditions(IConditionedEntity conditionedEntity)
    {
        if (conditionedEntity.Conditions is not null)
            foreach (var condition in conditionedEntity.Conditions)
                if (!MeetCondition(condition))
                    return false;

        return true;
    }

    public bool MeetCondition(ConditionEntity? condition)
    {
        // if there are no conditions to match, conditions are "meet"
        if (condition is null)
            return true;

        if (condition.Kind == Database.Enums.ConditionKind.Time)
        {
            return CheckTimeCondition(condition);
        }

        return false;
    }

    private bool CheckTimeCondition(ConditionEntity condition)
    {
        DateTime compareDateTime = DateTime.Today;

        if (condition.TimeMode == Database.Enums.TimeMode.Specified)
        {
            compareDateTime = compareDateTime.Add(condition?.TimeSpecified?.ToTimeSpan() ?? new TimeSpan(0));
        }
        else
        {
            var sunData = sunDataService.GetLatest();

            if (condition.TimeMode == Database.Enums.TimeMode.Sunrise)
            {
                compareDateTime = compareDateTime.Add(sunData.Sunrise.ToTimeSpan());
            }
            else
            {
                compareDateTime = compareDateTime.Add(sunData.Sunset.ToTimeSpan());
            }

            if (condition.TimeSpecified.HasValue)
                compareDateTime = compareDateTime.Add(condition.TimeSpecified.Value.ToTimeSpan());
        }

        bool result = false;
        if (condition?.TimeCompareKind == Database.Enums.CompareKind.GreaterThan)
        {
            result = DateTime.Now > compareDateTime;
        }
        else if (condition?.TimeCompareKind == Database.Enums.CompareKind.LesserThan)
        {
            result = compareDateTime > DateTime.Now;
        }

        return result;
    }
}
