using HomeAutomation.Database.Converters;
using HomeAutomation.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HomeAutomation.Database;

public class DefaultContext(DbContextOptions<DefaultContext> options) : DbContext(options)
{
    public DbSet<ActionEntity> Actions { get; set; }

    public DbSet<ActionDeviceEntity> ActionDevices { get; set; }

    public DbSet<ConditionEntity> Conditions { get; set; }

    public DbSet<DeviceEntity> Devices { get; set; }

    public DbSet<TriggerEntity> Triggers { get; set; }

    public DbSet<TriggerActionEntity> TriggerActions { get; set; }

    public DbSet<SensorValueEntity> SensorValues { get; set; }

    public DbSet<SunDataEntity> SunData { get; set; }

    public DbSet<WeatherForecastEntity> WeatherForecast { get; set; }

    public DbSet<LogEntity> Logs { get; set; }

    public DbSet<MailMessageEntity> MailMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DefaultContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<DateTime>().HaveConversion<DateTimeUTCConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableDateTimeUTCConverter>();
    }
}

public class DefaultContextFactory : IDesignTimeDbContextFactory<DefaultContext>
{
    public DefaultContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DefaultContext>()
            .UseNpgsql("");

        return new DefaultContext(optionsBuilder.Options);
    }
}
