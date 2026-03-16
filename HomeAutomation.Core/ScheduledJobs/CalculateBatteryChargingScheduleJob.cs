using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Core.Services;
using HomeAutomation.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeAutomation.Core.ScheduledJobs;

public class CalculateBatteryChargingScheduleJob(DefaultContext context, IFusionSolarService fusionSolarService, INotificationService notificationService, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<CalculateBatteryChargingScheduleJob> logger) : IScheduledJob
{
    private const string HOUR_FORMAT = "HH:mm";

    private const int CALCULATE_AFTER_HOUR = 14;
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
            // prices are estimated to release for tomorrow at 14
            if (currentExecution.Hour < CALCULATE_AFTER_HOUR)
                return;

            FetchedPricingRow[]? rows = null;
            DateOnly tomorrow = DateOnly.FromDateTime(currentExecution.AddDays(1));

            // check if we already have pricing in the database
            var pricing = await context.EnergyPricing.Where(x => x.Date == tomorrow).FirstOrDefaultAsync(cancellationToken);
            if (pricing != null)
            {
                if (pricing.IsConfigured)
                {
                    logger.LogInformation("Schedule.BatteryCharging :: pricing data for {tomorrow} is already configued", tomorrow);
                    return;
                }

                logger.LogInformation("Schedule.BatteryCharging :: pricing data for {tomorrow} fetch from database", tomorrow);
                rows = JsonSerializer.Deserialize<FetchedPricingRow[]>(pricing.PricingData);
            }

            if (rows == null || rows.Length == 0 || !rows.All(x => x.Definitive))
            {
                logger.LogInformation("Schedule.BatteryCharging :: fetching pricing data for {tomorrow} from eon", tomorrow);

                // pricing data is not in database or all are not definitive, fetch from api
                var client = httpClientFactory.CreateClient(nameof(CalculateBatteryChargingScheduleJob));
                var request = CreateMockedEnergyPricingRequest(tomorrow);

                var response = await client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    rows = await response.Content.ReadFromJsonAsync<FetchedPricingRow[]>(cancellationToken);
                }
                else
                {
                    string message = string.Empty;
                    try
                    {
                        message = await response.Content.ReadAsStringAsync(cancellationToken);
                    }
                    catch { }
                    logger.LogError("Schedule.BatteryCharging :: failed to fetch pricing data for {tomorrow} from eon, with message: {message}", tomorrow, message);
                }
            }

            // if we read from api, store to database
            if (pricing == null && rows != null)
            {
                logger.LogInformation("Schedule.BatteryCharging :: fetched new pricing data for {tomorrow} from eon, saving to database", tomorrow);

                pricing = new Database.Entities.EnergyPricingEntity
                {
                    Date = tomorrow,
                    PricingData = JsonSerializer.Serialize(rows)
                };
                context.EnergyPricing.Add(pricing);
                await context.SaveChangesAsync(cancellationToken);
            }
            else if (pricing != null)
            {
                logger.LogInformation("Schedule.BatteryCharging :: fetched updated pricing data for {tomorrow} from eon, saving to database", tomorrow);

                pricing.PricingData = JsonSerializer.Serialize(rows);
                await context.SaveChangesAsync(cancellationToken);
            }

            // we dont have real data to work with, or its not late enough yet to use not definitive data
            if (rows == null || rows.Length == 0 || (!rows.All(x => x.Definitive) && currentExecution.Hour < SET_CHARGING_AFTER_HOUR_WHEN_NOT_DEFINITIVE))
            {
                logger.LogInformation("Schedule.BatteryCharging :: no real pricing data for {tomorrow}, retry at a later time", tomorrow);
                return;
            }

            if (currentExecution.Hour < SET_CHARGING_AFTER_HOUR)
            {
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

    private HttpRequestMessage CreateMockedEnergyPricingRequest(DateOnly date)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, configuration.GetValue<string>("EnergyPrices:APIUrl") + date.ToString("yyyy-MM-dd"));

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("Accept-Language", "sv,en;q=0.9,en-GB;q=0.8,en-US;q=0.7");
        request.Headers.Add("Sec-CH-UA", "\"Chromium\";v=\"142\", \"Microsoft Edge\";v=\"142\", \"Not_A Brand\";v=\"99\"");
        request.Headers.Add("Sec-CH-UA-Mobile", "?0");
        request.Headers.Add("Sec-CH-UA-Platform", "\"Windows\"");
        request.Headers.Add("Sec-Fetch-Dest", "empty");
        request.Headers.Add("Sec-Fetch-Mode", "cors");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        // real browser-like User-Agent
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
        request.Headers.Referrer = new Uri(configuration.GetValue("EnergyPrices:Referrer", ""));

        return request;
    }

    private ChargeWindow GetCheapestPeriod(List<FetchedPricingRow> prices, TimeSpan windowStart, TimeSpan windowEnd, TimeSpan duration, decimal thresholdPrice)
    {
        // TODO: redo this calculation, find cheapest segment, and then add to intervall on both sides untill threshold value is not there anymore

        // grab rows within time range
        var windowPrices = prices
            .Where(p => p.DateTime.TimeOfDay >= windowStart && p.DateTime.TimeOfDay <= windowEnd)
            .OrderBy(p => p.DateTime)
            .ToList();

        if (windowPrices.Count == 0)
            throw new ArgumentException("prices does not contain valid values", nameof(prices));

        int segmentLengthMinutes = PRICING_SEGMENT_LENGTH_IN_MINUTES;

        // check if all rows are below threshold value, if so, just get whole intervall
        if (windowPrices.All(p => p.Value <= thresholdPrice))
        {
            return new ChargeWindow
            {
                Start = windowPrices.First().DateTime,
                End = windowPrices.Last().DateTime.AddMinutes(segmentLengthMinutes),
                High = windowPrices.OrderBy(x => x.Value).Last().Value,
                Low = windowPrices.OrderBy(x => x.Value).First().Value,
                Average = windowPrices.Average(x => x.Value)
            };
        }

        // find the cheapest time segment
        int periods = (int)(duration.TotalMinutes / segmentLengthMinutes);
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

        var bestSegment = windowPrices.Skip(bestIndex).Take(periods);
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

    public class FetchedPricingRow
    {
        [JsonPropertyName("dateTime")]
        public DateTime DateTime { get; set; }

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("definitive")]
        public bool Definitive { get; set; }
    }
}
