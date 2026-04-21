using HomeAutomation.Core.Services;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeAutomation.Core.BackgroundJobs;

public class IkeaDirigeraEventBackgroundService(IServiceScopeFactory serviceScopeFactory, IIkeaDirigeraService ikeaDirigeraService, ILogger<IkeaDirigeraEventBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var eventItem in ikeaDirigeraService.ListenToEvents(stoppingToken))
                {
                    if (string.IsNullOrWhiteSpace(eventItem.DeviceId))
                        continue;

                    _ = HandleEvent(eventItem, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "IkeaDirigera.Event :: Listener failed, reconnecting in 10 seconds");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task HandleEvent(Models.IkeaDirigeraEventModel eventItem, CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
        var triggerService = scope.ServiceProvider.GetRequiredService<ITriggerService>();

        var device = await deviceRepository.Get(DeviceSource.IkeaDirigera, eventItem.DeviceId, stoppingToken);
        if (device == null)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("IkeaDirigera.Event :: Unmapped source device {sourceId}, rawData: {rawData}", eventItem.DeviceId, eventItem.RawPayload);
            return;
        }

        DeviceEvent deviceEvent = DeviceEvent.Unknown;
        if (eventItem.Attributes.TryGetValue("isOn", out string? isOnValue) && bool.TryParse(isOnValue, out bool isOn))
            deviceEvent = isOn ? DeviceEvent.On : DeviceEvent.Off;

        if (eventItem.Attributes.TryGetValue("isOpen", out string? isOpenValue) && bool.TryParse(isOpenValue, out bool isOpen))
            deviceEvent = isOpen ? DeviceEvent.On : DeviceEvent.Off;

        if (deviceEvent != DeviceEvent.Unknown)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("IkeaDirigera.Event :: {deviceId} :: SourceId:{sourceId}, Mapped event: {deviceEvent}", device.Id, eventItem.DeviceId, deviceEvent);

            await triggerService.FireTriggersFromDevice(device, deviceEvent, stoppingToken);
            return;
        }

        SensorValueKind sensorValueKind = SensorValueKind.Unknown;
        decimal? sensorValueDecimals = null;
        if (eventItem.Attributes.TryGetValue("currentActivePower", out string? currentActivePowerValue) && decimal.TryParse(currentActivePowerValue, out decimal currentActivePower))
        {
            sensorValueKind = SensorValueKind.EnergyFlow;
            sensorValueDecimals = currentActivePower;
        }

        if (sensorValueKind != SensorValueKind.Unknown && sensorValueDecimals is not null)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("IkeaDirigera.Event :: {deviceId} :: SourceId:{sourceId}, Mapped sensor value: {sensorValueKind}={sensorValueDecimals}", device.Id, eventItem.DeviceId, sensorValueKind, sensorValueDecimals);

            var sensorValueService = scope.ServiceProvider.GetRequiredService<ISensorValueService>();
            await sensorValueService.AddValue(device.Id, sensorValueKind, sensorValueDecimals?.ToString() ?? "", eventItem.Timestamp, stoppingToken);
        }
    }
}
