using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Database;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace HomeAutomation.Core.ScheduledJobs;

internal class SummarizeSolarGenerationScheduleJob(DefaultContext context, ILogger<SummarizeSolarGenerationScheduleJob> logger) : IScheduledJob
{
    private const int LOOKBACK_DAYS = 30;

    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        var solarCellsDevice = await context.Devices.Where(x => x.Source == Database.Enums.DeviceSource.FusionSolar && x.SourceId.EndsWith("_panels")).FirstOrDefaultAsync(cancellationToken);
        if (solarCellsDevice is null)
        {
            logger.LogWarning("Schedule.SummarizeSolarGeneration :: no solar panel device found, skipping");
            return;
        }

        // Determine which of the last 30 days are already summarized
        var windowStart = DateOnly.FromDateTime(currentExecution.AddDays(-LOOKBACK_DAYS));
        var yesterday = DateOnly.FromDateTime(currentExecution.AddDays(-1));

        var existingSummaryDates = await context.SolarGenerationSummaries
            .Where(s => s.Date >= windowStart && s.Date <= yesterday)
            .Select(s => s.Date)
            .ToHashSetAsync(cancellationToken);

        // Collect only the days that are missing a summary
        var daysToProcess = Enumerable
            .Range(0, LOOKBACK_DAYS)
            .Select(offset => yesterday.AddDays(-offset))
            .Where(date => !existingSummaryDates.Contains(date))
            .ToList();

        if (daysToProcess.Count == 0)
        {
            logger.LogInformation("Schedule.SummarizeSolarGeneration :: all days up to date, nothing to do");
            return;
        }

        logger.LogInformation("Schedule.SummarizeSolarGeneration :: processing {count} missing day(s)", daysToProcess.Count);

        foreach (var date in daysToProcess)
        {
            var dayStartUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
            var dayEndUtc = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Local).ToUniversalTime();

            var readings = await context.SensorValues
                .Where(sv => sv.DeviceId == solarCellsDevice.Id
                          && sv.Type == SensorValueKind.EnergyFlow
                          && sv.Timestamp >= dayStartUtc
                          && sv.Timestamp <= dayEndUtc)
                .OrderBy(sv => sv.Timestamp)
                .Select(sv => new { sv.Timestamp, Value = decimal.Parse(sv.Value, CultureInfo.InvariantCulture) })
                .ToListAsync(cancellationToken);

            if (readings.Count < 2)
            {
                logger.LogInformation("Schedule.SummarizeSolarGeneration :: not enough readings for {date}, skipping", date);
                continue;
            }

            // Trapezoidal integration: (kW₁ + kW₂) / 2 × Δhours per interval
            decimal totalKwh = 0;
            for (int i = 1; i < readings.Count; i++)
            {
                decimal deltaHours = (decimal)(readings[i].Timestamp - readings[i - 1].Timestamp).TotalHours;
                totalKwh += (readings[i - 1].Value + readings[i].Value) / 2m * deltaHours;
            }

            context.SolarGenerationSummaries.Add(new SolarGenerationSummaryEntity
            {
                Date = date,
                Started = TimeOnly.FromDateTime(readings.Where(x => x.Value > 0).First().Timestamp.ToLocalTime()),
                Ended = TimeOnly.FromDateTime(readings.Where(x => x.Value > 0).Last().Timestamp.ToLocalTime()),
                TotalKwh = totalKwh,
            });

            logger.LogInformation("Schedule.SummarizeSolarGeneration :: {date} → {kwh:F3} kWh ({readings} readings)", date, totalKwh, readings.Count);
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Schedule.SummarizeSolarGeneration :: done");
    }
}
