namespace HomeAutomation.Entities;

public interface IEntity
{
    string UniqueID { get; }

    string ToSourceString();
}
