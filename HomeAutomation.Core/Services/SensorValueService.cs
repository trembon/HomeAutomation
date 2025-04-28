using HomeAutomation.Database;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;

namespace HomeAutomation.Core.Services;

public interface ISensorValueService
{
    Task AddValue(int deviceId, SensorValueKind type, string value, DateTime timestamp, CancellationToken cancellationToken);
}

public class SensorValueService(DefaultContext context) : ISensorValueService
{
    public async Task AddValue(int deviceId, SensorValueKind type, string value, DateTime timestamp, CancellationToken cancellationToken)
    {
        SensorValueEntity sensorValue = new()
        {
            DeviceId = deviceId,
            Type = type,
            Value = value,
            Timestamp = timestamp
        };

        await context.SensorValues.AddAsync(sensorValue, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
