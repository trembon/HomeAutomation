using HomeAutomation.Core.Constants;
using HomeAutomation.Core.Models;
using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Core.Services;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace HomeAutomation.Core.ScheduledJobs;

[ScheduledJob(120)]
public class FetchFusionSolarEnergyFlowScheduleJob(
    IFusionSolarService fusionSolarService,
    IDeviceRepository deviceRepository,
    ISensorValueService sensorValueService,
    IStateStoreService stateStoreService,
    ILogger<FetchFusionSolarEnergyFlowScheduleJob> logger) : IScheduledJob
{
    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        try
        {
            FusionSolarEnergyFlowModel? energyFlow = await fusionSolarService.GetEnergyFlow(cancellationToken);
            if (energyFlow is null)
            {
                logger.LogWarning("Schedule.FusionSolarEnergyFlow :: no valid energy flow was returned");
                return;
            }

            List<HomeAutomation.Database.Entities.DeviceEntity> devices = await deviceRepository
                .GetOfSource(DeviceSource.FusionSolar, cancellationToken);

            await SaveChangedValue(
                devices,
                $"{energyFlow.StationId}_panels",
                SensorValueKind.EnergyFlow,
                energyFlow.SolarGeneration,
                energyFlow.Timestamp,
                cancellationToken);
            await SaveChangedValue(
                devices,
                $"{energyFlow.StationId}_battery",
                SensorValueKind.EnergyFlow,
                energyFlow.BatteryFlow,
                energyFlow.Timestamp,
                cancellationToken);
            await SaveChangedValue(
                devices,
                $"{energyFlow.StationId}_battery",
                SensorValueKind.ChargeLevel,
                energyFlow.BatteryChargeLevel,
                energyFlow.Timestamp,
                cancellationToken);
            await SaveChangedValue(
                devices,
                $"{energyFlow.StationId}_grid",
                SensorValueKind.EnergyFlow,
                energyFlow.GridFlow,
                energyFlow.Timestamp,
                cancellationToken);
            await SaveChangedValue(
                devices,
                $"{energyFlow.StationId}_house",
                SensorValueKind.EnergyFlow,
                energyFlow.HouseConsumption,
                energyFlow.Timestamp,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Schedule.FusionSolarEnergyFlow :: check failed; the next scheduled check will retry");
        }
    }

    private async Task SaveChangedValue(
        IReadOnlyCollection<HomeAutomation.Database.Entities.DeviceEntity> devices,
        string sourceId,
        SensorValueKind valueKind,
        decimal value,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        HomeAutomation.Database.Entities.DeviceEntity? device = devices.FirstOrDefault(item =>
            item.Kind == DeviceKind.Sensor
            && string.Equals(item.SourceId, sourceId, StringComparison.Ordinal));
        if (device is null)
            return;

        string valueText = value.ToString(CultureInfo.InvariantCulture);
        string? currentValue = stateStoreService.Get(StateKeys.SensorValue(device.Id, valueKind));
        if (decimal.TryParse(currentValue, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal currentDecimal)
            && currentDecimal == value)
        {
            return;
        }

        await sensorValueService.AddValue(
            device.Id,
            valueKind,
            valueText,
            timestamp.UtcDateTime,
            cancellationToken);
    }
}
