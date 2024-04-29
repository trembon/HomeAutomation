using HomeAutomation.Database;
using HomeAutomation.Database.Enums;
using HomeAutomation.Entities;
using HomeAutomation.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HomeAutomation.ScheduledJobs
{
    [DisallowConcurrentExecution]
    public class ImportWeatherDataScheduledJob : IJob
    {
        private readonly ILogger<ImportWeatherDataScheduledJob> logger;

        private readonly DefaultContext context;
        private readonly ISunDataService sunDataService;
        private readonly IConfiguration configuration;

        public ImportWeatherDataScheduledJob(DefaultContext context, ISunDataService sunDataService, IConfiguration configuration, ILogger<ImportWeatherDataScheduledJob> logger)
        {
            this.logger = logger;
            this.context = context;
            this.sunDataService = sunDataService;
            this.configuration = configuration;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogInformation("Schedule.Weather :: starting");

            WeatherData weatherData = null;
            try
            {
                using(var client = new WebClient())
                {
                    using (Stream data = await client.OpenReadTaskAsync(configuration["Weather:DataUrl"]))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(WeatherData));
                        weatherData = (WeatherData)serializer.Deserialize(data);
                    }
                }
            }
            catch(Exception ex)
            {
                logger.LogError(ex, $"Failed to fetch weather and sun information.");
            }

            if (weatherData == null)
                return;

            // insert sun rise and set event data
            try
            {
                sunDataService.Add(weatherData.Sun.Rise.Date, weatherData.Sun.Rise, weatherData.Sun.Set);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to store sun information in database.");
            }


            // import weather forecast
            try
            {
                logger.LogInformation("Schedule.Weather :: updating weather");

                DateTime date = weatherData.Forecast.Tabular.Time.OrderBy(t => t.From).Select(t => t.From.Date).FirstOrDefault();
                var previousForecast = this.context.WeatherForecast.OrderBy(wf => wf.Date).ThenBy(wf => wf.Period).Where(wf => wf.Date >= date).ToList();

                foreach(var item in weatherData.Forecast.Tabular.Time)
                {
                    WeatherForecast forecast = previousForecast.FirstOrDefault(f => f.Date == item.From.Date && (int)f.Period == item.Period);
                    if(forecast == null)
                    {
                        forecast = new WeatherForecast();
                        this.context.Add(forecast);
                    }

                    forecast.Date = item.From.Date;
                    forecast.Period = (WeatherForecastPeriod)item.Period;
                    forecast.SymbolID = item.Symbol.Var;

                    forecast.Temperature = item.Temperature.Value;

                    forecast.WindSpeed = item.WindSpeed.Mps;
                    forecast.WindDirection = item.WindDirection.Code;

                    forecast.Rain = item.Precipitation.Value;
                }

                this.context.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to store weather information in database.");
            }
        }
    }
}
