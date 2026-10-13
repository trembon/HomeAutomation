using HomeAutomation.Core.Constants;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;

namespace HomeAutomation.Core.Services;

public interface ISensorValueService
{
    Task AddValue(int deviceId, SensorValueKind valueKind, string value, DateTime? timestamp, CancellationToken cancellationToken);

    Dictionary<int, Dictionary<string, string>> GetCurrentValues(IReadOnlyCollection<int> deviceIds);
}

public class SensorValueService(IRepository<SensorValueEntity> repository, IStateStoreService stateStoreService) : ISensorValueService
{
    public Dictionary<int, Dictionary<string, string>> GetCurrentValues(IReadOnlyCollection<int> deviceIds)
    {
        Dictionary<int, Dictionary<string, string>> currentValues = [];
        SensorValueKind[] valueKinds = [.. Enum.GetValues<SensorValueKind>()
            .Where(valueKind => valueKind != SensorValueKind.Unknown)];

        foreach (int deviceId in deviceIds.Distinct())
        {
            Dictionary<string, string> deviceValues = [];
            foreach (SensorValueKind valueKind in valueKinds)
            {
                string? value = stateStoreService.Get(StateKeys.SensorValue(deviceId, valueKind));
                if (value is not null)
                    deviceValues[valueKind.ToString()] = value;
            }

            currentValues[deviceId] = deviceValues;
        }

        return currentValues;
    }

    public async Task AddValue(int deviceId, SensorValueKind valueKind, string value, DateTime? timestamp, CancellationToken cancellationToken)
    {
        await repository.AddAndSave(new()
        {
            DeviceId = deviceId,
            Type = valueKind,
            Value = value,
            Timestamp = timestamp ?? DateTime.UtcNow
        }, cancellationToken);

        await stateStoreService.SetSensorValue(deviceId, valueKind, value, cancellationToken);
    }
}
