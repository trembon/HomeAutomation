using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeAutomation.Database.Entities;

public class SolarGenerationSummaryEntity : BaseEntity
{
    public DateOnly Date { get; set; }

    public TimeOnly Started { get; set; }

    public TimeOnly Ended { get; set; }

    /// <summary>
    /// Total estimated solar energy generated for the day in kWh.
    /// </summary>
    public decimal TotalKwh { get; set; }
}

public class SolarGenerationSummaryConfiguration : IEntityTypeConfiguration<SolarGenerationSummaryEntity>
{
    public void Configure(EntityTypeBuilder<SolarGenerationSummaryEntity> builder)
    {
        builder.HasIndex(s => s.Date).IsUnique();
    }
}
