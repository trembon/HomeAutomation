using HomeAutomation.Database.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeAutomation.Database.Entities;

public class SensorDailySummaryEntity : BaseEntity
{
    public int DeviceId { get; set; }

    public DeviceEntity Device { get; set; } = null!;

    public SensorValueKind Type { get; set; }

    public DateOnly Date { get; set; }

    public decimal MinValue { get; set; }

    public decimal MaxValue { get; set; }

    public decimal AvgValue { get; set; }

    public decimal FirstValue { get; set; }

    public decimal LastValue { get; set; }

    public int ReadingCount { get; set; }
}

public class SensorDailySummaryConfiguration : IEntityTypeConfiguration<SensorDailySummaryEntity>
{
    public void Configure(EntityTypeBuilder<SensorDailySummaryEntity> builder)
    {
        builder.HasIndex(x => new { x.DeviceId, x.Type, x.Date }).IsUnique();
        builder.HasIndex(x => x.Date);
    }
}
