using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Interfaces;
using System.Text.RegularExpressions;

namespace HomeAutomation.Core.Services;

public interface IEvaluateConditionService
{
    bool MeetConditions(IConditionedEntity conditionedEntity);

    bool MeetCondition(ConditionEntity? condition);
}

public partial class EvaluateConditionService(ISunDataService sunDataService) : IEvaluateConditionService
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

        if (condition.Kind == Database.Enums.ConditionKind.Expression)
        {
            return CheckExpressionCondition(condition);
        }

        return false;
    }

    #region Time condition
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
    #endregion

    #region Expression condition
    // Expression format: {variable} op value [AND {variable} op value]* [OR ...]
    //
    // Supported variables:
    //   {sunrise}              - today's sunrise time
    //   {sunset}               - today's sunset time
    //   {time}                 - current local time
    //   {state:key}            - global state value (future)
    //   {sensor:deviceId:kind} - latest sensor value for a device (future)
    //
    // Supported operators: < > =
    // AND binds tighter than OR (standard precedence)

    [GeneratedRegex(@"^\{([^}]+)\}\s*([<>=])\s*(.+)$", RegexOptions.Compiled)]
    private static partial Regex AtomPatternRegex();

    // Matches: {variable} op value  e.g. {sunrise} < 6:15
    private static readonly Regex ExpressionAtomPattern = AtomPatternRegex();

    private bool CheckExpressionCondition(ConditionEntity condition)
    {
        if (string.IsNullOrWhiteSpace(condition.Expression))
            return false;

        return EvaluateExpression(condition.Expression);
    }

    private bool EvaluateExpression(string expression)
    {
        string[] orGroups = expression.Split([" OR "], StringSplitOptions.TrimEntries);
        foreach (string orGroup in orGroups)
        {
            if (EvaluateAndGroup(orGroup))
                return true;
        }
        return false;
    }

    private bool EvaluateAndGroup(string andGroup)
    {
        string[] atoms = andGroup.Split([" AND "], StringSplitOptions.TrimEntries);
        foreach (string atom in atoms)
        {
            if (!EvaluateAtom(atom))
                return false;
        }
        return true;
    }

    private bool EvaluateAtom(string atom)
    {
        var match = ExpressionAtomPattern.Match(atom);
        if (!match.Success)
            return false;

        string variableExpr = match.Groups[1].Value;
        string op = match.Groups[2].Value;
        string rawValue = match.Groups[3].Value.Trim();

        object? left = ResolveExpressionVariable(variableExpr);
        if (left is null)
            return false;

        return CompareExpressionValues(left, op, rawValue);
    }

    private object? ResolveExpressionVariable(string variableExpr)
    {
        string[] parts = variableExpr.Split(':');
        return parts[0].ToLowerInvariant() switch
        {
            "sunrise" => sunDataService.GetLatest().Sunrise,
            "sunset" => sunDataService.GetLatest().Sunset,
            "time" => TimeOnly.FromTimeSpan(DateTime.Now.TimeOfDay),
            _ => null
        };
    }

    private static bool CompareExpressionValues(object left, string op, string rawRight)
    {
        if (left is TimeOnly leftTime && TimeOnly.TryParse(rawRight, out var rightTime))
        {
            return op switch
            {
                "<" => leftTime < rightTime,
                ">" => leftTime > rightTime,
                "=" => leftTime == rightTime,
                _ => false
            };
        }

        return false;
    }
    #endregion
}
