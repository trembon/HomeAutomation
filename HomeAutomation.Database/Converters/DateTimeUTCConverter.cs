using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HomeAutomation.Database.Converters;

internal class DateTimeUTCConverter : ValueConverter<DateTime, DateTime>
{
    public DateTimeUTCConverter()
        : base(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
