using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Core.Services;
using HomeAutomation.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeAutomation.Core.ScheduledJobs;

[ScheduledJob(3600)]
public class CalculateBatteryChargingScheduleJob(DefaultContext context, IFusionSolarService fusionSolarService, INotificationService notificationService, ILogger<CalculateBatteryChargingScheduleJob> logger) : IScheduledJob
{
    private const string HOUR_FORMAT = "HH:mm";

    private const int SET_CHARGING_AFTER_HOUR = 18;
    private const int SET_CHARGING_AFTER_HOUR_WHEN_NOT_DEFINITIVE = 22;
    private const int CHARGING_THRESHOLD_PRICE = 20;
    private const double NIGHT_CHARGING_START_HOUR = 0;
    private const double NIGHT_CHARGING_END_HOUR = 5.5;
    private const double NIGHT_CHARGING_PERIOD_LENGTH = 3;
    private const double DAY_CHARGING_START_HOUR = 10;
    private const double DAY_CHARGING_END_HOUR = 16.5;
    private const double DAY_CHARGING_PERIOD_LENGTH = 2;
    private const int PRICING_SEGMENT_LENGTH_IN_MINUTES = 15;

    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        logger.LogInformation("Schedule.BatteryCharging :: starting");
        try
        {
            DateOnly tomorrow = DateOnly.FromDateTime(currentExecution.AddDays(1));

            var pricing = await context.EnergyPricing.Where(x => x.Date == tomorrow).FirstOrDefaultAsync(cancellationToken);
            if (pricing == null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Schedule.BatteryCharging :: no pricing data for {tomorrow}, retry at a later time", tomorrow);
                return;
            }

            if (pricing.IsConfigured)
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Schedule.BatteryCharging :: pricing data for {tomorrow} is already configured", tomorrow);
                return;
            }

            var rows = JsonSerializer.Deserialize<FetchedPricingRow[]>(pricing.PricingData);

            // we dont have real data to work with, or its not late enough yet to use not definitive data
            if (rows == null || rows.Length == 0 || (!pricing.AllDefinitive && currentExecution.Hour < SET_CHARGING_AFTER_HOUR_WHEN_NOT_DEFINITIVE))
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Schedule.BatteryCharging :: no real pricing data for {tomorrow}, retry at a later time", tomorrow);
                return;
            }

            if (currentExecution.Hour < SET_CHARGING_AFTER_HOUR)
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Schedule.BatteryCharging :: not late enough to configure battery schedule for {tomorrow}, wait", tomorrow);
                return;
            }

            var night = GetCheapestPeriod([.. rows],
                TimeSpan.FromHours(NIGHT_CHARGING_START_HOUR),
                TimeSpan.FromHours(NIGHT_CHARGING_END_HOUR),
                TimeSpan.FromHours(NIGHT_CHARGING_PERIOD_LENGTH),
                CHARGING_THRESHOLD_PRICE);

            var day = GetCheapestPeriod([.. rows],
                TimeSpan.FromHours(DAY_CHARGING_START_HOUR),
                TimeSpan.FromHours(DAY_CHARGING_END_HOUR),
                TimeSpan.FromHours(DAY_CHARGING_PERIOD_LENGTH),
                CHARGING_THRESHOLD_PRICE);

            string discharge1start = day.End.AddMinutes(1).ToString(HOUR_FORMAT);
            string discharge1end = night.Start.AddMinutes(-1).ToString(HOUR_FORMAT);
            string discharge2start = night.End.AddMinutes(1).ToString(HOUR_FORMAT);
            string discharge2end = day.Start.AddMinutes(-1).ToString(HOUR_FORMAT);

            List<ScheduleItem> schedule = [
                new ScheduleItem { StartTime = night.Start.ToString(HOUR_FORMAT), EndTime = night.End.ToString(HOUR_FORMAT), OnOff = 0 },
                new ScheduleItem { StartTime = discharge2start, EndTime = discharge2end, OnOff = 1 },
                new ScheduleItem { StartTime = day.Start.ToString(HOUR_FORMAT), EndTime = day.End.ToString(HOUR_FORMAT), OnOff = 0 },
                new ScheduleItem { StartTime = discharge1start, EndTime = discharge1end, OnOff = 1 }
            ];
            var payload = new SchedulePayload { Id = "230190101", Value = JsonSerializer.Serialize(schedule) };

            bool success = await fusionSolarService.SetConfigSignals(payload, cancellationToken);
            if (success)
            {
                pricing!.IsConfigured = true;
                context.SaveChanges();

                await notificationService.SendToSlack("event", $"Battery charging schema updated:\n{night}\n{day}", cancellationToken);

                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Schedule.BatteryCharging :: configuration for {tomorrow} is now set", tomorrow);
            }
            else
            {
                logger.LogError("Schedule.BatteryCharging :: failed to update battery schedule for {tomorrow}", tomorrow);
            }
        }
        finally
        {
            logger.LogInformation("Schedule.BatteryCharging :: done");
        }
    }

    private ChargeWindow GetCheapestPeriod(List<FetchedPricingRow> prices, TimeSpan windowStart, TimeSpan windowEnd, TimeSpan duration, decimal thresholdPrice)
    {
        // grab rows within time range
        var windowPrices = prices
            .Where(p => p.DateTime.TimeOfDay >= windowStart && p.DateTime.TimeOfDay <= windowEnd)
            .OrderBy(p => p.DateTime)
            .ToList();

        if (windowPrices.Count == 0)
            throw new ArgumentException("prices does not contain valid values", nameof(prices));

        int segmentLengthMinutes = PRICING_SEGMENT_LENGTH_IN_MINUTES;
        int periods = (int)(duration.TotalMinutes / segmentLengthMinutes);

        // find the cheapest fixed-duration segment
        decimal minAvg = decimal.MaxValue;
        int bestIndex = 0;

        for (int i = 0; i <= windowPrices.Count - periods; i++)
        {
            var segment = windowPrices.Skip(i).Take(periods);
            decimal avg = segment.Average(p => p.Value);

            if (avg < minAvg)
            {
                minAvg = avg;
                bestIndex = i;
            }
        }

        int startIndex = bestIndex;
        int endIndex = bestIndex + periods - 1;

        // extend left while within window and price is at or below threshold
        while (startIndex > 0 && windowPrices[startIndex - 1].Value <= thresholdPrice)
            startIndex--;

        // extend right while within window and price is at or below threshold
        while (endIndex < windowPrices.Count - 1 && windowPrices[endIndex + 1].Value <= thresholdPrice)
            endIndex++;

        var bestSegment = windowPrices.Skip(startIndex).Take(endIndex - startIndex + 1);
        return new ChargeWindow
        {
            Start = bestSegment.First().DateTime,
            End = bestSegment.Last().DateTime.AddMinutes(segmentLengthMinutes),
            High = bestSegment.OrderBy(x => x.Value).Last().Value,
            Low = bestSegment.OrderBy(x => x.Value).First().Value,
            Average = bestSegment.Average(x => x.Value)
        };
    }

    private class ChargeWindow
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Average { get; set; }

        public override string ToString()
        {
            return $"{Start.TimeOfDay} - {End.TimeOfDay} Avg: {Average:F2}, Low: {Low:F2}, High: {High:F2}";
        }
    }

    public class ScheduleItem
    {
        [JsonPropertyName("startTime")]
        public string StartTime { get; set; } = null!;

        [JsonPropertyName("repeatPeriod")]
        public List<int> RepeatPeriod { get; set; } = [7, 1, 2, 3, 4, 5, 6];

        [JsonPropertyName("endTime")]
        public string EndTime { get; set; } = null!;

        [JsonPropertyName("onOff")]
        public int OnOff { get; set; }
    }

    public class SchedulePayload
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("value")]
        public string Value { get; set; } = null!;
    }

}
