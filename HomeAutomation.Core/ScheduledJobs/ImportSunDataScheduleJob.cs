using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace HomeAutomation.Core.ScheduledJobs;

public class ImportSunDataScheduleJob(ILogger<ImportSunDataScheduleJob> logger, IConfiguration configuration, ISunDataService sunDataService) : IScheduledJob
{
    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        logger.LogInformation("Schedule.SunData :: starting");

        try
        {
            string sunDataUrl = string.Format(configuration["Forecasts:SunDataUrl"] ?? "", configuration["Forecasts:Lat"], configuration["Forecasts:Lng"]);

            HttpClient client = new();
            using var response = await client.GetAsync(sunDataUrl, cancellationToken);

            var data = await response.Content.ReadFromJsonAsync<SunDataResponse>(cancellationToken);
            if (data != null && data.Results != null)
                _ = sunDataService.Add(DateOnly.FromDateTime(data.Results.Sunrise), TimeOnly.FromDateTime(data.Results.Sunrise.ToLocalTime()), TimeOnly.FromDateTime(data.Results.Sunset.ToLocalTime()));

            logger.LogInformation("Schedule.SunData :: done");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schedule.SunData :: failed to fetch and update sundata :: Error: {Message}", ex.Message);
        }
    }

    public class SunDataResponse
    {
        public required string Status { get; set; }

        public required SunDataResultResponse Results { get; set; }
    }

    public class SunDataResultResponse
    {
        public DateTime Sunrise { get; set; }

        public DateTime Sunset { get; set; }
    }
}
