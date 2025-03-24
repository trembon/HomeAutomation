using HomeAutomation.Database;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;

namespace HomeAutomation.Core.Services;

public interface ISensorValueService
{
    Task AddValue(DeviceSource source, string sourceId, SensorValueType type, string value, DateTime timestamp);
}

public class SensorValueService(DefaultContext context) : ISensorValueService
{
    public async Task AddValue(DeviceSource source, string sourceId, SensorValueType type, string value, DateTime timestamp)
    {
        SensorValue sensorValue = new()
        {
            Source = source,
            SourceID = sourceId,
            Type = type,
            Value = value,
            Timestamp = timestamp
        };

        await context.SensorValues.AddAsync(sensorValue);
        await context.SaveChangesAsync();
    }
}
