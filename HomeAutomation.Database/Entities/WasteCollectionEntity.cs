using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeAutomation.Database.Entities;

public class WasteCollectionEntity : BaseEntity
{
    public string ExternalAddressId { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string City { get; set; } = null!;

    public string PropertyId { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateOnly CollectionDate { get; set; }

    public string Precision { get; set; } = null!;

    public DateTime FirstSeenAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime? NotificationSentAt { get; set; }
}

public class WasteCollectionEntityConfiguration : IEntityTypeConfiguration<WasteCollectionEntity>
{
    public void Configure(EntityTypeBuilder<WasteCollectionEntity> builder)
    {
        builder.HasIndex(x => new { x.ExternalAddressId, x.Type, x.CollectionDate }).IsUnique();
        builder.HasIndex(x => new { x.CollectionDate, x.IsActive });

        builder.Property(x => x.ExternalAddressId).HasMaxLength(64);
        builder.Property(x => x.Address).HasMaxLength(512);
        builder.Property(x => x.City).HasMaxLength(128);
        builder.Property(x => x.PropertyId).HasMaxLength(64);
        builder.Property(x => x.Type).HasMaxLength(128);
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.Precision).HasMaxLength(32);
    }
}
