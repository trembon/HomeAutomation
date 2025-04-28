namespace HomeAutomation.Database.Entities;

public class TriggerActionEntity
{
    public int Id { get; set; }

    public int TriggerId { get; set; }

    public TriggerEntity Trigger { get; set; } = null!;

    public int ActionId { get; set; }

    public ActionEntity Action { get; set; } = null!;
}
