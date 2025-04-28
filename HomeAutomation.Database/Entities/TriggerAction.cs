namespace HomeAutomation.Database.Entities;

public class TriggerAction
{
    public int Id { get; set; }

    public int TriggerId { get; set; }

    public Trigger Trigger { get; set; } = null!;

    public int ActionId { get; set; }

    public Action Action { get; set; } = null!;
}
