using Microsoft.EntityFrameworkCore;

namespace HomeAutomation.Database
{
    public class DefaultContext : DbContext
    {
        public DbSet<SensorValue> SensorValues { get; set; }

        public DbSet<SunData> SunData { get; set; }

        public DbSet<WeatherForecast> WeatherForecast { get; set; }

        public DbSet<PhoneCall> PhoneCalls { get; set; }

        public DefaultContext(DbContextOptions<DefaultContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SunData>().HasIndex(sd => sd.Date).IsUnique();
        }
    }
}
