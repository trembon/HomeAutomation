using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Logging;

namespace HomeAutomation.Database.Entities;

public class LogEntity : BaseEntity
{
    public LogLevel Level { get; set; }

    public string Category { get; set; } = null!;

    public int EventID { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string Message { get; set; } = null!;

    public string? Exception { get; set; }
}

public class LogEntityConfiguration : IEntityTypeConfiguration<LogEntity>
{
    public void Configure(EntityTypeBuilder<LogEntity> builder)
    {
        builder.HasIndex(x => x.Category);
    }
}
