using HomeAutomation.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Database.Contexts;

public class LogContext(DbContextOptions<LogContext> options) : DbContext(options)
{
    public DbSet<LogRow> Rows { get; set; }

    public DbSet<MailMessage> MailMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LogRow>().Property(sp => sp.Timestamp).HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc).ToLocalTime());
        modelBuilder.Entity<MailMessage>().Property(sp => sp.Timestamp).HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc).ToLocalTime());
    }
}
