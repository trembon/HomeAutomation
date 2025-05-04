using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeAutomation.Database.Entities;

public class SunDataEntity : BaseEntity
{
    public DateOnly Date { get; set; }

    public TimeOnly Sunset { get; set; }

    public TimeOnly Sunrise { get; set; }
}

public class SunDataConfiguration : IEntityTypeConfiguration<SunDataEntity>
{
    public void Configure(EntityTypeBuilder<SunDataEntity> builder)
    {
        builder.HasIndex(sd => sd.Date).IsUnique();
    }
}
