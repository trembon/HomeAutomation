﻿using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace HomeAutomation.Database.Entities;

public class LogRow
{
    [Key]
    [Required]
    public int ID { get; set; }

    [Required]
    public LogLevel Level { get; set; }

    public string Category { get; set; } = null!;

    public int EventID { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;

    public string Message { get; set; } = null!;

    public string? Exception { get; set; }
}
