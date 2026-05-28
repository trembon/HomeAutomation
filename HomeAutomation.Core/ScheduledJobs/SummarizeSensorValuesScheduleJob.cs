using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Database;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace HomeAutomation.Core.ScheduledJobs;

[ScheduledJob(7200)]
public class SummarizeSensorValuesScheduleJob(DefaultContext context, ILogger<SummarizeSensorValuesScheduleJob> logger) : IScheduledJob
{
    private static readonly SensorValueKind[] SupportedTypes =
    [
        SensorValueKind.Temperature,
        SensorValueKind.Humidity,
        SensorValueKind.EnergyFlow,
        SensorValueKind.ChargeLevel,
    ];

    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        var yesterday = DateOnly.FromDateTime(currentExecution.AddDays(-1));
        var earliestSensorDate = await context.SensorValues
            .Where(x => SupportedTypes.Contains(x.Type))
            .OrderBy(x => x.Timestamp)
            .Select(x => (DateTime?)x.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        if (earliestSensorDate is null)
        {
            logger.LogInformation("Schedule.SummarizeSensorValues :: no numeric sensor values found, skipping");
            return;
        }

        var startDate = DateOnly.FromDateTime(earliestSensorDate.Value.ToLocalTime());
        if (startDate > yesterday)
        {
            logger.LogInformation("Schedule.SummarizeSensorValues :: no completed day to summarize yet");
            return;
        }

        var existingDates = await context.SensorDailySummaries
            .Where(x => x.Date >= startDate && x.Date <= yesterday)
            .Select(x => x.Date)
            .Distinct()
            .ToHashSetAsync(cancellationToken);

        var datesToProcess = new List<DateOnly>();
        for (var date = startDate; date <= yesterday; date = date.AddDays(1))
        {
            if (!existingDates.Contains(date) || date == yesterday)
                datesToProcess.Add(date);
        }

        if (datesToProcess.Count == 0)
        {
            logger.LogInformation("Schedule.SummarizeSensorValues :: all days up to date, nothing to do");
            return;
        }

        logger.LogInformation("Schedule.SummarizeSensorValues :: processing {count} day(s) from {fromDate} to {toDate}", datesToProcess.Count, datesToProcess.First(), datesToProcess.Last());

        foreach (var date in datesToProcess)
            await SummarizeDate(date, cancellationToken);

        logger.LogInformation("Schedule.SummarizeSensorValues :: done");
    }

    private async Task SummarizeDate(DateOnly date, CancellationToken cancellationToken)
    {
        var dayStartUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
        var dayEndUtc = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Local).ToUniversalTime();

        var rawValues = await context.SensorValues
            .Where(x => SupportedTypes.Contains(x.Type)
                     && x.Timestamp >= dayStartUtc
                     && x.Timestamp <= dayEndUtc)
            .OrderBy(x => x.Timestamp)
            .Select(x => new RawSensorValueRow(x.DeviceId, x.Type, x.Timestamp, x.Value))
            .ToListAsync(cancellationToken);

        List<ParsedSensorValueRow> parsedValues = [];
        foreach (var rawValue in rawValues)
        {
            if (TryParse(rawValue, out var parsed))
                parsedValues.Add(parsed!);
        }

        var groupedSummaries = parsedValues
            .GroupBy(x => new { x.DeviceId, x.Type })
            .Select(group =>
            {
                var ordered = group.OrderBy(x => x.Timestamp).ToList();
                return new SensorDailySummaryEntity
                {
                    DeviceId = group.Key.DeviceId,
                    Type = group.Key.Type,
                    Date = date,
                    MinValue = ordered.Min(x => x.Value),
                    MaxValue = ordered.Max(x => x.Value),
                    AvgValue = ordered.Average(x => x.Value),
                    FirstValue = ordered.First().Value,
                    LastValue = ordered.Last().Value,
                    ReadingCount = ordered.Count,
                };
            })
            .ToList();

        var existingRows = await context.SensorDailySummaries
            .Where(x => x.Date == date)
            .ToListAsync(cancellationToken);

        if (existingRows.Count > 0)
            context.SensorDailySummaries.RemoveRange(existingRows);

        if (groupedSummaries.Count > 0)
            await context.SensorDailySummaries.AddRangeAsync(groupedSummaries, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Schedule.SummarizeSensorValues :: {date} -> {summaryCount} summaries from {readingCount} valid readings", date, groupedSummaries.Count, parsedValues.Count);
    }

    private static bool TryParse(RawSensorValueRow raw, out ParsedSensorValueRow? parsed)
    {
        if (decimal.TryParse(raw.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            parsed = new ParsedSensorValueRow(raw.DeviceId, raw.Type, raw.Timestamp, value);
            return true;
        }

        parsed = default;
        return false;
    }

    private sealed record RawSensorValueRow(int DeviceId, SensorValueKind Type, DateTime Timestamp, string Value);

    private sealed record ParsedSensorValueRow(int DeviceId, SensorValueKind Type, DateTime Timestamp, decimal Value);
}
