using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Database;
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

    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        logger.LogInformation("Schedule.FetchEnergyPricing :: starting");
        try
        {
            // prices are estimated to release for tomorrow at 14
            if (currentExecution.Hour < CALCULATE_AFTER_HOUR)
                return;

            DateOnly tomorrow = DateOnly.FromDateTime(currentExecution.AddDays(1));

            var pricing = await context.EnergyPricing.Where(x => x.Date == tomorrow).FirstOrDefaultAsync(cancellationToken);
            if (pricing != null && pricing.IsConfigured)
            {
                logger.LogInformation("Schedule.FetchEnergyPricing :: pricing data for {tomorrow} is already configured", tomorrow);
                return;
            }

            FetchedPricingRow[]? rows = null;
            if (pricing != null)
            {
                logger.LogInformation("Schedule.FetchEnergyPricing :: pricing data for {tomorrow} fetch from database", tomorrow);
                rows = JsonSerializer.Deserialize<FetchedPricingRow[]>(pricing.PricingData);
            }

            if (rows == null || rows.Length == 0 || !rows.All(x => x.Definitive))
            {
                logger.LogInformation("Schedule.FetchEnergyPricing :: fetching pricing data for {tomorrow} from eon", tomorrow);

                var client = httpClientFactory.CreateClient(nameof(FetchEnergyPricingScheduleJob));
                var request = CreateEnergyPricingRequest(tomorrow);

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
                    logger.LogError("Schedule.FetchEnergyPricing :: failed to fetch pricing data for {tomorrow} from eon, with message: {message}", tomorrow, message);
                }
            }

            if (pricing == null && rows != null)
            {
                logger.LogInformation("Schedule.FetchEnergyPricing :: fetched new pricing data for {tomorrow} from eon, saving to database", tomorrow);

                pricing = new Database.Entities.EnergyPricingEntity
                {
                    Date = tomorrow,
                    PricingData = JsonSerializer.Serialize(rows)
                };
                context.EnergyPricing.Add(pricing);
                await context.SaveChangesAsync(cancellationToken);
            }
            else if (pricing != null && rows != null)
            {
                logger.LogInformation("Schedule.FetchEnergyPricing :: fetched updated pricing data for {tomorrow} from eon, saving to database", tomorrow);

                pricing.PricingData = JsonSerializer.Serialize(rows);
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        finally
        {
            logger.LogInformation("Schedule.FetchEnergyPricing :: done");
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
