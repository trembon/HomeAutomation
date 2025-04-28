using HomeAutomation.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeAutomation.Database;

public class DefaultContext(DbContextOptions<DefaultContext> options) : DbContext(options)
{
    public DbSet<Entities.ActionEntity> Actions { get; set; }

    public DbSet<ConditionEntity> Conditions { get; set; }

    public DbSet<DeviceEntity> Devices { get; set; }

    public DbSet<TriggerEntity> Triggers { get; set; }

    public DbSet<SensorValueEntity> SensorValues { get; set; }

    public DbSet<SunDataEntity> SunData { get; set; }

    public DbSet<WeatherForecastEntity> WeatherForecast { get; set; }

    public DbSet<LogEntity> Logs { get; set; }

    public DbSet<MailMessageEntity> MailMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SunDataEntity>().HasIndex(sd => sd.Date).IsUnique();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var dateTimeProperties = entityType.GetProperties().Where(p => p.ClrType == typeof(DateTime));
            foreach (var property in dateTimeProperties)
                modelBuilder.Entity(entityType.Name).Property<DateTime>(property.Name).HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        }
    }
}
