using HomeAutomation.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeAutomation.Database.Contexts;

public class DefaultContext(DbContextOptions<DefaultContext> options) : DbContext(options)
{
    public DbSet<SensorValue> SensorValues { get; set; }

    public DbSet<SunData> SunData { get; set; }

    public DbSet<WeatherForecast> WeatherForecast { get; set; }

    public DbSet<PhoneCall> PhoneCalls { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SunData>().HasIndex(sd => sd.Date).IsUnique();

        //modelBuilder.Entity<SunData>().Property(sd => sd.Date).HasConversion(x => x.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var dateTimeProperties = entityType.GetProperties().Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?));

            foreach (var property in dateTimeProperties)
            {
                modelBuilder.Entity(entityType.Name).Property<DateTime>(property.Name).HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            }
        }
    }
}
