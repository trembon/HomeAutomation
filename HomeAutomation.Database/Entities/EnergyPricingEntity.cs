namespace HomeAutomation.Database.Entities;

public class EnergyPricingEntity : BaseEntity
{
    public DateOnly Date { get; set; }

    public string PricingData { get; set; } = null!;

    public bool IsConfigured { get; set; }
}
