using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeAutomation.Database.Entities;

public class StateValueEntity : BaseEntity
{
    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }

    public int? DeviceId { get; set; }

    public DeviceEntity? Device { get; set; }
}

public class StateValueEntityConfiguration : IEntityTypeConfiguration<StateValueEntity>
{
    public void Configure(EntityTypeBuilder<StateValueEntity> builder)
    {
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => x.DeviceId);

        builder.Property(x => x.Key).HasMaxLength(256);
        builder.Property(x => x.Value).HasMaxLength(256);
    }
}
