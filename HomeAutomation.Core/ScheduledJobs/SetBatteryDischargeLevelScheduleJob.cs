using HomeAutomation.Core.Constants;
using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Core.Services;
using HomeAutomation.Database;
using HomeAutomation.Database.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json.Serialization;

namespace HomeAutomation.Core.ScheduledJobs;

[ScheduledJob(21600)]
public class SetBatteryDischargeLevelScheduleJob(DefaultContext context, IStateStoreService stateStoreService, IFusionSolarService fusionSolarService, INotificationService notificationService, ILogger<SetBatteryDischargeLevelScheduleJob> logger) : IScheduledJob
{
    private const int LOOKBACK_DAYS = 5;
    private const decimal SUMMER_TEMPERATURE_THRESHOLD = 11m;
    private const int SUMMER_DISCHARGE_LEVEL = 3;
    private const int WINTER_DISCHARGE_LEVEL = 10;
    private const string FUSION_SOLAR_SIGNAL_ID = "230320278";
    private const string FUSION_SOLAR_TYPE = "battery";
    private const string SUMMER_MODE = "summer";
    private const string WINTER_MODE = "winter";

    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        logger.LogInformation("Schedule.BatteryDischargeLevel :: starting");

        try
        {
            string deviceIdStateKey = StateKeys.CustomState(StateKeys.BatteryRoomTemperatureDeviceId);
            string? deviceIdValue = stateStoreService.Get(deviceIdStateKey);
            if (!int.TryParse(deviceIdValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int deviceId))
            {
                logger.LogWarning("Schedule.BatteryDischargeLevel :: missing or invalid {stateKey} custom state value", deviceIdStateKey);
                return;
            }

            var referenceDate = DateOnly.FromDateTime(currentExecution);
            var fromDate = referenceDate.AddDays(-LOOKBACK_DAYS);

            var summaries = await context.SensorDailySummaries
                .AsNoTracking()
                .Where(x => x.DeviceId == deviceId
                         && x.Type == SensorValueKind.Temperature
                         && x.Date >= fromDate
                         && x.Date < referenceDate
                         && x.ReadingCount > 0)
                .Select(x => new TemperatureSummary(x.AvgValue, x.ReadingCount))
                .ToListAsync(cancellationToken);

            int totalReadings = summaries.Sum(x => x.ReadingCount);
            if (totalReadings == 0)
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Schedule.BatteryDischargeLevel :: no temperature summaries found for device {deviceId} from {fromDate} to {toDate}", deviceId, fromDate, referenceDate.AddDays(-1));
                return;
            }

            decimal averageTemperature = summaries.Sum(x => x.AvgValue * x.ReadingCount) / totalReadings;
            var desiredMode = averageTemperature > SUMMER_TEMPERATURE_THRESHOLD
                ? new BatteryDischargeMode(SUMMER_MODE, SUMMER_DISCHARGE_LEVEL)
                : new BatteryDischargeMode(WINTER_MODE, WINTER_DISCHARGE_LEVEL);

            string modeStateKey = StateKeys.CustomState(StateKeys.BatteryDischargeLevelMode);
            string? currentMode = stateStoreService.Get(modeStateKey);
            if (string.Equals(currentMode, desiredMode.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Schedule.BatteryDischargeLevel :: {mode} mode already applied, average temperature {averageTemperature:F2}C", desiredMode.Name, averageTemperature);
                return;
            }

            var payload = new ConfigSignalPayload
            {
                Id = FUSION_SOLAR_SIGNAL_ID,
                Value = desiredMode.DischargeLevel.ToString(CultureInfo.InvariantCulture)
            };

            bool success = await fusionSolarService.SetConfigSignals(FUSION_SOLAR_TYPE, payload, cancellationToken);
            if (!success)
            {
                logger.LogError("Schedule.BatteryDischargeLevel :: failed to set {mode} mode discharge level {level} with average temperature {averageTemperature:F2}C", desiredMode.Name, desiredMode.DischargeLevel, averageTemperature);
                return;
            }

            await stateStoreService.Set(modeStateKey, desiredMode.Name, null, cancellationToken);
            await notificationService.SendToSlack("event", $"Battery discharge level set to {desiredMode.DischargeLevel} ({desiredMode.Name} mode) based on average room temperature of {averageTemperature:F2}C", cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Schedule.BatteryDischargeLevel :: set {mode} mode discharge level {level} with average temperature {averageTemperature:F2}C", desiredMode.Name, desiredMode.DischargeLevel, averageTemperature);
        }
        finally
        {
            logger.LogInformation("Schedule.BatteryDischargeLevel :: done");
        }
    }

    private sealed record TemperatureSummary(decimal AvgValue, int ReadingCount);

    private sealed record BatteryDischargeMode(string Name, int DischargeLevel);

    private sealed class ConfigSignalPayload
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("value")]
        public string Value { get; set; } = null!;
    }
}
