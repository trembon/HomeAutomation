using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HomeAutomation.Database.Converters;

internal class NullableDateTimeUTCConverter : ValueConverter<DateTime?, DateTime?>
{
    public override bool ConvertsNulls => true;

    public NullableDateTimeUTCConverter()
        : base(
            v => v.HasValue ? v.Value.ToUniversalTime() : null,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null)
    {
    }
}
