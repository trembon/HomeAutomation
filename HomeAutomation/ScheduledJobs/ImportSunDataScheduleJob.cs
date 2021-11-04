using HomeAutomation.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace HomeAutomation.ScheduledJobs
{
    [DisallowConcurrentExecution]
    public class ImportSunDataScheduleJob : IJob
    {
        private readonly ILogger<ImportSunDataScheduleJob> logger;
        private readonly IConfiguration configuration;
        private readonly ISunDataService sunDataService;

        public ImportSunDataScheduleJob(ILogger<ImportSunDataScheduleJob> logger, IConfiguration configuration, ISunDataService sunDataService)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.sunDataService = sunDataService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogInformation("Schedule.SunData :: starting");

            try
            {
                string sunDataUrl = string.Format(configuration["Forecasts:SunDataUrl"], configuration["Forecasts:Lat"], configuration["Forecasts:Lng"]);

                using(var client = new HttpClient())
                {
                    using(var response = await client.GetAsync(sunDataUrl))
                    {
                        var data = await response.Content.ReadAsAsync<SunDataResponse>();
                        sunDataService.Add(data.Results.Sunrise.Date, data.Results.Sunrise.ToLocalTime(), data.Results.Sunset.ToLocalTime());
                    }
                }

                logger.LogInformation("Schedule.SunData :: done");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Schedule.SunData :: failed to fetch and update sundata :: Error:{ex.Message}");
            }
        }

        public class SunDataResponse
        {
            public string Status { get; set; }

            public SunDataResultResponse Results { get; set; }
        }

        public class SunDataResultResponse
        {
            public DateTime Sunrise { get; set; }

            public DateTime Sunset { get; set; }
        }
    }
}
