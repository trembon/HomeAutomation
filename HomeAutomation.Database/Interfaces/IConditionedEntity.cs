using HomeAutomation.Database.Entities;

namespace HomeAutomation.Database.Interfaces;

public interface IConditionedEntity
{
    List<ConditionEntity> Conditions { get; set; }
}
