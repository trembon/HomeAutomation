using HomeAutomation.Database.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeAutomation.Database.Entities;

public class SensorValueEntity : BaseEntity
{
    public int DeviceId { get; set; }

    public DeviceEntity Device { get; set; } = null!;

    public SensorValueKind Type { get; set; }

    public string Value { get; set; } = null!;

    public DateTime Timestamp { get; set; }
}

public class SensorValueConfiguration : IEntityTypeConfiguration<SensorValueEntity>
{
    public void Configure(EntityTypeBuilder<SensorValueEntity> builder)
    {
        builder.HasIndex(x => x.DeviceId);
        builder.HasIndex(x => new { x.DeviceId, x.Type, x.Timestamp });
    }
}
