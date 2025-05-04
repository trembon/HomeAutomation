using HomeAutomation.Database.Entities;

namespace HomeAutomation.Database.Interfaces;

public interface IConditionedEntity
{
    int? ConditionId { get; set; }

    ConditionEntity? Condition { get; set; }
}
