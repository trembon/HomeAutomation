using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Database;
using HomeAutomation.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeAutomation.Core.ScheduledJobs;

[ScheduledJob(3600)]
public class FetchEnergyPricingScheduleJob(DefaultContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<FetchEnergyPricingScheduleJob> logger) : IScheduledJob
{
    private const int CALCULATE_AFTER_HOUR = 14;
    private const int BACKFILL_DAYS = 30;

    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        logger.LogInformation("Schedule.FetchEnergyPricing :: starting");
        try
        {
            // re-fetch any historical days within the past 30 days that are not yet fully definitive
            var thirtyDaysAgo = DateOnly.FromDateTime(currentExecution.AddDays(-BACKFILL_DAYS));
            var today = DateOnly.FromDateTime(currentExecution);

            var nonDefinitive = await context.EnergyPricing
                .Where(x => x.Date >= thirtyDaysAgo && x.Date <= today && !x.AllDefinitive)
                .OrderByDescending(x => x.Date)
                .FirstOrDefaultAsync(cancellationToken);

            if (nonDefinitive != null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Schedule.FetchEnergyPricing :: re-fetching non-definitive pricing data for {date}", nonDefinitive.Date);

                await FetchAndSaveForDate(nonDefinitive.Date, nonDefinitive, cancellationToken);
            }

            // prices for tomorrow are estimated to release at 14
            if (currentExecution.Hour < CALCULATE_AFTER_HOUR)
                return;

            DateOnly tomorrow = DateOnly.FromDateTime(currentExecution.AddDays(1));
            var tomorrowPricing = await context.EnergyPricing.Where(x => x.Date == tomorrow).FirstOrDefaultAsync(cancellationToken);

            if (tomorrowPricing != null && tomorrowPricing.AllDefinitive)
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Schedule.FetchEnergyPricing :: pricing data for {tomorrow} is already definitive", tomorrow);
                return;
            }

            await FetchAndSaveForDate(tomorrow, tomorrowPricing, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schedule.FetchEnergyPricing :: error while fetching energy pricing data");
        }
        finally
        {
            logger.LogInformation("Schedule.FetchEnergyPricing :: done");
        }
    }

    private async Task FetchAndSaveForDate(DateOnly date, EnergyPricingEntity? existing, CancellationToken cancellationToken)
    {
        FetchedPricingRow[]? rows = null;
        if (existing != null)
            rows = JsonSerializer.Deserialize<FetchedPricingRow[]>(existing.PricingData);

        if (rows == null || rows.Length == 0 || !rows.All(x => x.Definitive))
        {
            logger.LogInformation("Schedule.FetchEnergyPricing :: fetching pricing data for {date} from eon", date);

            var client = httpClientFactory.CreateClient(nameof(FetchEnergyPricingScheduleJob));
            var request = CreateEnergyPricingRequest(date);

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
                logger.LogError("Schedule.FetchEnergyPricing :: failed to fetch pricing data for {date} from eon, with message: {message}", date, message);
                return;
            }
        }

        if (rows == null || rows.Length == 0)
            return;

        bool allDefinitive = rows.All(x => x.Definitive);

        if (existing == null)
        {
            logger.LogInformation("Schedule.FetchEnergyPricing :: fetched new pricing data for {date} from eon, saving to database", date);

            context.EnergyPricing.Add(new EnergyPricingEntity
            {
                Date = date,
                PricingData = JsonSerializer.Serialize(rows),
                AllDefinitive = allDefinitive
            });
            await context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            logger.LogInformation("Schedule.FetchEnergyPricing :: fetched updated pricing data for {date} from eon, saving to database", date);

            existing.PricingData = JsonSerializer.Serialize(rows);
            existing.AllDefinitive = allDefinitive;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private HttpRequestMessage CreateEnergyPricingRequest(DateOnly date)
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
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
        request.Headers.Referrer = new Uri(configuration.GetValue("EnergyPrices:Referrer", ""));

        return request;
    }
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
