using HomeAutomation.Core.Constants;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HomeAutomation.Core.Services;

public interface IStateStoreService
{
    Task EnsureInitialized(CancellationToken cancellationToken);

    string? Get(string key);

    Task Set(string key, string value, int? deviceId, CancellationToken cancellationToken);

    Task SetDeviceState(int deviceId, DeviceEvent deviceEvent, CancellationToken cancellationToken);

    Task SetSensorValue(int deviceId, SensorValueKind valueKind, string value, CancellationToken cancellationToken);
}

public class StateStoreService(IServiceScopeFactory serviceScopeFactory, ILogger<StateStoreService> logger) : IStateStoreService
{
    private readonly ConcurrentDictionary<string, StateValueEntity> _stateValues = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private volatile bool _initialized;

    public string? Get(string key)
    {
        EnsureInitialized();
        return _stateValues.TryGetValue(key, out var stateValue) ? stateValue.Value : null;
    }

    public async Task Set(string key, string value, int? deviceId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        await EnsureInitialized(cancellationToken);

        var now = DateTime.UtcNow;
        _stateValues.AddOrUpdate(
            key,
            _ => new StateValueEntity
            {
                Key = key,
                Value = value,
                UpdatedAt = now,
                DeviceId = deviceId
            },
            (_, existing) =>
            {
                existing.Value = value;
                existing.UpdatedAt = now;
                existing.DeviceId = deviceId;
                return existing;
            });

        using var scope = serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IStateValueRepository>();

        var persisted = await repository.GetByKey(key, cancellationToken);
        if (persisted is null)
        {
            await repository.AddAndSave(new StateValueEntity
            {
                Key = key,
                Value = value,
                UpdatedAt = now,
                DeviceId = deviceId
            }, cancellationToken);
            return;
        }

        persisted.Value = value;
        persisted.UpdatedAt = now;
        persisted.DeviceId = deviceId;
        await repository.Save(cancellationToken);
    }

    public Task SetDeviceState(int deviceId, DeviceEvent deviceEvent, CancellationToken cancellationToken)
    {
        if (deviceEvent == DeviceEvent.Unknown)
            return Task.CompletedTask;

        return Set(StateKeys.DeviceState(deviceId), deviceEvent.ToString(), deviceId, cancellationToken);
    }

    public Task SetSensorValue(int deviceId, SensorValueKind valueKind, string value, CancellationToken cancellationToken)
    {
        return Set(StateKeys.SensorValue(deviceId, valueKind), value, deviceId, cancellationToken);
    }

    public void EnsureInitialized()
    {
        if (_initialized)
            return;

        EnsureInitialized(CancellationToken.None).GetAwaiter().GetResult();
    }

    public async Task EnsureInitialized(CancellationToken cancellationToken)
    {
        if (_initialized)
            return;

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
                return;

            using var scope = serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IStateValueRepository>();
            var persistedValues = await repository.GetAll(cancellationToken);
            foreach (var persistedValue in persistedValues)
                _stateValues[persistedValue.Key] = persistedValue;

            _initialized = true;

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("StateStore.Initialize :: Loaded {count} current state values", persistedValues.Count);
        }
        finally
        {
            _initializationLock.Release();
        }
    }
}
