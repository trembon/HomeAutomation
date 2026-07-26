using HomeAutomation.Database.Converters;
using HomeAutomation.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HomeAutomation.Database;

public class DefaultContext(DbContextOptions<DefaultContext> options) : DbContext(options)
{
    public DbSet<HouseListingEntity> HouseListings => Set<HouseListingEntity>();

    public DbSet<ActionEntity> Actions => Set<ActionEntity>();

    public DbSet<ActionDeviceEntity> ActionDevices => Set<ActionDeviceEntity>();

    public DbSet<ConditionEntity> Conditions => Set<ConditionEntity>();

    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();

    public DbSet<EnergyPricingEntity> EnergyPricing => Set<EnergyPricingEntity>();

    public DbSet<TriggerEntity> Triggers => Set<TriggerEntity>();

    public DbSet<TriggerActionEntity> TriggerActions => Set<TriggerActionEntity>();

    public DbSet<SensorValueEntity> SensorValues => Set<SensorValueEntity>();

    public DbSet<SensorDailySummaryEntity> SensorDailySummaries => Set<SensorDailySummaryEntity>();

    public DbSet<StateValueEntity> StateValues => Set<StateValueEntity>();

    public DbSet<SolarGenerationSummaryEntity> SolarGenerationSummaries => Set<SolarGenerationSummaryEntity>();

    public DbSet<SunDataEntity> SunData => Set<SunDataEntity>();

    public DbSet<WeatherForecastEntity> WeatherForecast => Set<WeatherForecastEntity>();

    public DbSet<WasteCollectionEntity> WasteCollections => Set<WasteCollectionEntity>();

    public DbSet<LogEntity> Logs => Set<LogEntity>();

    public DbSet<MailMessageEntity> MailMessages => Set<MailMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DefaultContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
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
