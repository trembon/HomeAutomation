using Microsoft.Extensions.Logging;

namespace HomeAutomation.Database.Entities;

public class LogEntity
{
    public int Id { get; set; }

    public LogLevel Level { get; set; }

    public string Category { get; set; } = null!;

    public int EventID { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string Message { get; set; } = null!;

    public string? Exception { get; set; }
}
