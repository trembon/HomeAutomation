using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;

namespace HomeAutomation.Core.Services;

public interface ISensorValueService
{
    Task AddValue(int deviceId, SensorValueKind valueKind, string value, DateTime? timestamp, CancellationToken cancellationToken);
}

public class SensorValueService(IRepository<SensorValueEntity> repository) : ISensorValueService
{
    public async Task AddValue(int deviceId, SensorValueKind valueKind, string value, DateTime? timestamp, CancellationToken cancellationToken)
    {
        await repository.AddAndSave(new()
        {
            DeviceId = deviceId,
            Type = valueKind,
            Value = value,
            Timestamp = timestamp ?? DateTime.UtcNow
        }, cancellationToken);
    }
}
