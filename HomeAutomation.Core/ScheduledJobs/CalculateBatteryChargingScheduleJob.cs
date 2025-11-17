using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Core.Services;
using HomeAutomation.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeAutomation.Core.ScheduledJobs;

public class CalculateBatteryChargingScheduleJob(DefaultContext context, IFusionSolarService fusionSolarService, INotificationService notificationService, IHttpClientFactory httpClientFactory, ILogger<CalculateBatteryChargingScheduleJob> logger) : IScheduledJob
{
    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        logger.LogInformation("Schedule.BatteryCharging :: starting");
        try
        {
            // prices are estimated to release for tomorrow at 14
            // TODO: config?
            if (currentExecution.Hour < 14)
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

            if (rows == null || !rows.All(x => x.Definitive))
            {
                logger.LogInformation("Schedule.BatteryCharging :: fetching pricing data for {tomorrow} from eon", tomorrow);
                // pricing data is not in database or all are not definitive
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

            // we dont have real data to work with
            if (rows == null || !rows.All(x => x.Definitive))
            {
                logger.LogInformation("Schedule.BatteryCharging :: no real pricing data for {tomorrow}, retry at a later time", tomorrow);
                return;
            }

            if (currentExecution.Hour < 18)
            {
                logger.LogInformation("Schedule.BatteryCharging :: not late enough to configure battery schedule for {tomorrow}, wait", tomorrow);
                return;
            }

            // TODO: config on all of these values?
            var night = GetCheapestPeriod([.. rows], TimeSpan.FromHours(0), TimeSpan.FromHours(5.5), TimeSpan.FromHours(3), 20);
            var day = GetCheapestPeriod([.. rows], TimeSpan.FromHours(10), TimeSpan.FromHours(16.5), TimeSpan.FromHours(2), 20);

            string discharge1start = day.End.AddMinutes(1).ToString("hh:mm");
            string discharge1end = night.Start.AddMinutes(-1).ToString("hh:mm");
            string discharge2start = night.End.AddMinutes(1).ToString("hh:mm");
            string discharge2end = day.Start.AddMinutes(-1).ToString("hh:mm");

            List<ScheduleItem> schedule = [
                new ScheduleItem { StartTime = night.Start.ToString("hh:mm"), EndTime = night.End.ToString("hh:mm"), OnOff = 0 },
                new ScheduleItem { StartTime = discharge2start, EndTime = discharge2end, OnOff = 1 },
                new ScheduleItem { StartTime = day.Start.ToString("hh:mm"), EndTime = day.End.ToString("hh:mm"), OnOff = 0 },
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

    private static HttpRequestMessage CreateMockedEnergyPricingRequest(DateOnly date)
    {
        // TODO: config?
        var request = new HttpRequestMessage(HttpMethod.Get, "https://eonepapirun.azurewebsites.net/api/getSpotPrices?priceArea=SE4&date=" + date.ToString("yyyy-MM-dd"));

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

        // TODO: config?
        request.Headers.Referrer = new Uri("https://www.eon.se/el/elpriser/aktuella");

        return request;
    }

    private static ChargeWindow GetCheapestPeriod(List<FetchedPricingRow> prices, TimeSpan windowStart, TimeSpan windowEnd, TimeSpan duration, decimal thresholdPrice)
    {
        // grab rows within time range
        var windowPrices = prices
            .Where(p => p.DateTime.TimeOfDay >= windowStart && p.DateTime.TimeOfDay <= windowEnd)
            .OrderBy(p => p.DateTime)
            .ToList();

        if (windowPrices.Count == 0)
            throw new ArgumentException("prices does not contain valid values", nameof(prices));

        // check if all rows are below threshold value, if so, just get whole intervall
        // TODO: redo this calculation, find cheapest segment, and then add to intervall on both sides untill threshold value is not there anymore
        if (windowPrices.All(p => p.Value <= thresholdPrice))
        {
            return new ChargeWindow
            {
                Start = windowPrices.First().DateTime,
                End = windowPrices.Last().DateTime.AddMinutes(15), // TODO: config on segment length (15minutes)
                High = windowPrices.OrderBy(x => x.Value).Last().Value,
                Low = windowPrices.OrderBy(x => x.Value).First().Value,
                Average = windowPrices.Average(x => x.Value)
            };
        }

        // find the cheapest time segment
        // TODO: config on segment length (15minutes)
        int periods = (int)(duration.TotalMinutes / 15);
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
            End = bestSegment.Last().DateTime.AddMinutes(15),
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
            return $"{Start.TimeOfDay} - {End.TimeOfDay} Avg: {Average}kr, Low: {Low}kr, High: {High}kr";
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
