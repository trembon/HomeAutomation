using HomeAutomation.Core.Services;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using NSubstitute;

namespace HomeAutomation.Core.Tests.Services;

public class EvaluateConditionServiceTests
{
    private readonly ISunDataService _sunDataService;
    private readonly EvaluateConditionService _sut;

    public EvaluateConditionServiceTests()
    {
        _sunDataService = Substitute.For<ISunDataService>();
        _sut = new EvaluateConditionService(_sunDataService);
    }

    #region MeetConditions

    [Fact]
    public void MeetConditions_NoConditions_ReturnsTrue()
    {
        var trigger = new TriggerEntity { Conditions = [] };

        Assert.True(_sut.MeetConditions(trigger).isMet);
    }

    [Fact]
    public void MeetConditions_AllConditionsMet_ReturnsTrue()
    {
        SetupSunData(sunrise: new TimeOnly(5, 0), sunset: new TimeOnly(21, 0));
        var trigger = new TriggerEntity
        {
            Conditions =
            [
                ExpressionCondition("{sunrise} < 6:00"),
                ExpressionCondition("{sunset} > 20:00")
            ]
        };

        Assert.True(_sut.MeetConditions(trigger).isMet);
    }

    [Fact]
    public void MeetConditions_OneConditionFails_ReturnsFalse()
    {
        SetupSunData(sunrise: new TimeOnly(7, 0), sunset: new TimeOnly(21, 0));
        var trigger = new TriggerEntity
        {
            Conditions =
            [
                ExpressionCondition("{sunrise} < 6:00"),
                ExpressionCondition("{sunset} > 20:00")
            ]
        };

        Assert.False(_sut.MeetConditions(trigger).isMet);
    }

    #endregion

    #region MeetCondition

    [Fact]
    public void MeetCondition_NullCondition_ReturnsTrue()
    {
        Assert.True(_sut.MeetCondition(null).isMet);
    }

    [Fact]
    public void MeetCondition_UnknownKind_ReturnsFalse()
    {
        var condition = new ConditionEntity { Kind = (ConditionKind)99 };

        Assert.False(_sut.MeetCondition(condition).isMet);
    }

    #endregion

    #region Expression — variables

    [Fact]
    public void MeetCondition_Expression_SunriseBeforeThreshold_ReturnsTrue()
    {
        SetupSunData(sunrise: new TimeOnly(5, 0));
        var condition = ExpressionCondition("{sunrise} < 6:15");

        Assert.True(_sut.MeetCondition(condition).isMet);
    }

    [Fact]
    public void MeetCondition_Expression_SunriseAfterThreshold_ReturnsFalse()
    {
        SetupSunData(sunrise: new TimeOnly(7, 0));
        var condition = ExpressionCondition("{sunrise} < 6:15");

        Assert.False(_sut.MeetCondition(condition).isMet);
    }

    [Fact]
    public void MeetCondition_Expression_SunriseEqualToThreshold_ReturnsFalse()
    {
        // strict less-than: equal does not satisfy <
        SetupSunData(sunrise: new TimeOnly(6, 15));
        var condition = ExpressionCondition("{sunrise} < 6:15");

        Assert.False(_sut.MeetCondition(condition).isMet);
    }

    [Fact]
    public void MeetCondition_Expression_SunsetAfterThreshold_ReturnsTrue()
    {
        SetupSunData(sunset: new TimeOnly(21, 0));
        var condition = ExpressionCondition("{sunset} > 20:00");

        Assert.True(_sut.MeetCondition(condition).isMet);
    }

    [Fact]
    public void MeetCondition_Expression_SunsetBeforeThreshold_ReturnsFalse()
    {
        SetupSunData(sunset: new TimeOnly(19, 0));
        var condition = ExpressionCondition("{sunset} > 20:00");

        Assert.False(_sut.MeetCondition(condition).isMet);
    }

    [Fact]
    public void MeetCondition_Expression_SunriseEqualsValue_ReturnsTrue()
    {
        SetupSunData(sunrise: new TimeOnly(6, 0));
        var condition = ExpressionCondition("{sunrise} = 6:00");

        Assert.True(_sut.MeetCondition(condition).isMet);
    }

    [Fact]
    public void MeetCondition_Expression_SunriseNotEqualToValue_ReturnsFalse()
    {
        SetupSunData(sunrise: new TimeOnly(6, 30));
        var condition = ExpressionCondition("{sunrise} = 6:00");

        Assert.False(_sut.MeetCondition(condition).isMet);
    }

    #endregion

    #region Expression — AND / OR

    [Fact]
    public void MeetCondition_Expression_AndBothTrue_ReturnsTrue()
    {
        SetupSunData(sunrise: new TimeOnly(5, 0), sunset: new TimeOnly(21, 0));
        var condition = ExpressionCondition("{sunrise} < 6:00 AND {sunset} > 20:00");

        Assert.True(_sut.MeetCondition(condition).isMet);
    }

    [Fact]
    public void MeetCondition_Expression_AndFirstFalse_ReturnsFalse()
    {
        SetupSunData(sunrise: new TimeOnly(7, 0), sunset: new TimeOnly(21, 0));
        var condition = ExpressionCondition("{sunrise} < 6:00 AND {sunset} > 20:00");

        Assert.False(_sut.MeetCondition(condition).isMet);
    }

    [Fact]
    public void MeetCondition_Expression_AndSecondFalse_ReturnsFalse()
    {
        SetupSunData(sunrise: new TimeOnly(5, 0), sunset: new TimeOnly(19, 0));
        var condition = ExpressionCondition("{sunrise} < 6:00 AND {sunset} > 20:00");

        Assert.False(_sut.MeetCondition(condition).isMet);
    }

    [Fact]
    public void MeetCondition_Expression_OrFirstTrue_ReturnsTrue()
    {
        SetupSunData(sunrise: new TimeOnly(5, 0), sunset: new TimeOnly(19, 0));
        var condition = ExpressionCondition("{sunrise} < 6:00 OR {sunset} > 20:00");

        Assert.True(_sut.MeetCondition(condition).isMet);
    }

    [Fact]
    public void MeetCondition_Expression_OrSecondTrue_ReturnsTrue()
    {
        SetupSunData(sunrise: new TimeOnly(7, 0), sunset: new TimeOnly(21, 0));
        var condition = ExpressionCondition("{sunrise} < 6:00 OR {sunset} > 20:00");

        Assert.True(_sut.MeetCondition(condition).isMet);
    }

    [Fact]
    public void MeetCondition_Expression_OrBothFalse_ReturnsFalse()
    {
        SetupSunData(sunrise: new TimeOnly(7, 0), sunset: new TimeOnly(19, 0));
        var condition = ExpressionCondition("{sunrise} < 6:00 OR {sunset} > 20:00");

        Assert.False(_sut.MeetCondition(condition).isMet);
    }

    [Fact]
    public void MeetCondition_Expression_AndOrPrecedence_AndEvaluatedBeforeOr()
    {
        // ({sunrise} < 6:00 AND {sunset} > 20:00) OR {sunrise} < 8:00
        // sunrise=7:00, sunset=21:00 → (false AND true) OR true → true
        SetupSunData(sunrise: new TimeOnly(7, 0), sunset: new TimeOnly(21, 0));
        var condition = ExpressionCondition("{sunrise} < 6:00 AND {sunset} > 20:00 OR {sunrise} < 8:00");

        Assert.True(_sut.MeetCondition(condition).isMet);
    }

    #endregion

    #region Expression — edge cases

    [Fact]
    public void MeetCondition_Expression_NullExpression_ReturnsFalse()
    {
        var condition = new ConditionEntity { Kind = ConditionKind.Expression, Expression = null };

        Assert.False(_sut.MeetCondition(condition).isMet);
    }

    [Fact]
    public void MeetCondition_Expression_EmptyExpression_ReturnsFalse()
    {
        var condition = ExpressionCondition("");

        Assert.False(_sut.MeetCondition(condition).isMet);
    }

    [Fact]
    public void MeetCondition_Expression_UnknownVariable_ReturnsFalse()
    {
        var condition = ExpressionCondition("{unknown} < 6:00");

        Assert.False(_sut.MeetCondition(condition).isMet);
    }

    [Fact]
    public void MeetCondition_Expression_InvalidFormat_ReturnsFalse()
    {
        var condition = ExpressionCondition("not a valid expression");

        Assert.False(_sut.MeetCondition(condition).isMet);
    }

    #endregion

    private static ConditionEntity ExpressionCondition(string expression) =>
        new() { Kind = ConditionKind.Expression, Expression = expression };

    private void SetupSunData(TimeOnly? sunrise = null, TimeOnly? sunset = null)
    {
        _sunDataService.GetLatest().Returns(new SunDataEntity
        {
            Sunrise = sunrise ?? new TimeOnly(6, 0),
            Sunset = sunset ?? new TimeOnly(20, 0)
        });
    }
}
