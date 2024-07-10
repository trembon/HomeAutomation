using HomeAutomation.Core.Entities;

namespace HomeAutomation.Entities.Conditions
{
    public abstract class Condition : IEntity
    {
        public string UniqueID => $"{nameof(Condition)}_";

        public abstract Task<bool> Check(ConditionExecutionArguments arguments);

        public string ToSourceString()
        {
            return null;
        }
    }
}
