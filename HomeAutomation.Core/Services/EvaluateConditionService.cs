using HomeAutomation.Core.Constants;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Interfaces;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HomeAutomation.Core.Services;

public interface IEvaluateConditionService
{
    (bool isMet, string name) MeetConditions(IConditionedEntity conditionedEntity);

    (bool isMet, string name) MeetCondition(ConditionEntity? condition);
}

public partial class EvaluateConditionService(ISunDataService sunDataService, IStateStoreService stateStoreService) : IEvaluateConditionService
{
    public (bool isMet, string name) MeetConditions(IConditionedEntity conditionedEntity)
    {
        if (conditionedEntity.Conditions is not null)
            foreach (var condition in conditionedEntity.Conditions)
            {
                var status = MeetCondition(condition);
                if (!status.isMet)
                    return status;
            }

        return (true, string.Empty);
    }

    public (bool isMet, string name) MeetCondition(ConditionEntity? condition)
    {
        // if there are no conditions to match, conditions are "meet"
        if (condition is null)
            return (true, string.Empty);

        bool isMet = false;
        if (condition.Kind == Database.Enums.ConditionKind.Time)
        {
            isMet = CheckTimeCondition(condition);
        }

        if (condition.Kind == Database.Enums.ConditionKind.Expression)
        {
            isMet = CheckExpressionCondition(condition);
        }

        return (isMet, condition.Name);
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
            "device" => ResolveDeviceState(parts),
            "sensor" => ResolveSensorValue(parts),
            "state" => ResolveCustomState(parts),
            _ => null
        };
    }

    private string? ResolveDeviceState(string[] parts)
    {
        if (parts.Length != 3 || !int.TryParse(parts[1], out int deviceId) || !parts[2].Equals("state", StringComparison.InvariantCultureIgnoreCase))
            return null;

        return stateStoreService.Get(StateKeys.DeviceState(deviceId));
    }

    private string? ResolveSensorValue(string[] parts)
    {
        if (parts.Length != 3 || !int.TryParse(parts[1], out int deviceId) || !Enum.TryParse<SensorValueKind>(parts[2], true, out var valueKind))
            return null;

        return stateStoreService.Get(StateKeys.SensorValue(deviceId, valueKind));
    }

    private string? ResolveCustomState(string[] parts)
    {
        if (parts.Length < 2)
            return null;

        string stateName = string.Join(':', parts[1..]);
        return stateStoreService.Get(StateKeys.CustomState(stateName));
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

        if (left is string leftString && decimal.TryParse(leftString, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal leftDecimal)
            && decimal.TryParse(rawRight, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal rightDecimal))
        {
            return op switch
            {
                "<" => leftDecimal < rightDecimal,
                ">" => leftDecimal > rightDecimal,
                "=" => leftDecimal == rightDecimal,
                _ => false
            };
        }

        if (left is string leftValue && op == "=")
            return string.Equals(leftValue, rawRight, StringComparison.InvariantCultureIgnoreCase);

        return false;
    }
    #endregion
}
