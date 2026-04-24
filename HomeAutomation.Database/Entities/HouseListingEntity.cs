using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeAutomation.Database.Entities;

public class HouseListingEntity : BaseEntity
{
    public string ExternalId { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string Price { get; set; } = string.Empty;

    public string Info { get; set; } = string.Empty;

    public string Url { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime FirstSeen { get; set; }

    public DateTime LastSeen { get; set; }

    public DateTime LastChanged { get; set; }
}

public class BooliListingEntityConfiguration : IEntityTypeConfiguration<HouseListingEntity>
{
    public void Configure(EntityTypeBuilder<HouseListingEntity> builder)
    {
        builder.HasIndex(x => x.ExternalId).IsUnique();
        builder.Property(x => x.Address).HasMaxLength(512);
        builder.Property(x => x.Price).HasMaxLength(128);
        builder.Property(x => x.Url).HasMaxLength(1024);
        builder.Property(x => x.Status).HasMaxLength(32);
    }
}
