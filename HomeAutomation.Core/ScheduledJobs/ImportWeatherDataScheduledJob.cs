using HomeAutomation.Core.Models.Weather;
using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Core.Services;
using HomeAutomation.Database.Contexts;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Xml.Serialization;

namespace HomeAutomation.ScheduledJobs;

public class ImportWeatherDataScheduledJob(DefaultContext context, ISunDataService sunDataService, IConfiguration configuration, ILogger<ImportWeatherDataScheduledJob> logger) : IScheduledJob
{
    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        logger.LogInformation("Schedule.Weather :: starting");

        WeatherData weatherData = null;
        try
        {
            using(var client = new WebClient())
            {
                using (Stream data = await client.OpenReadTaskAsync(configuration["Forecasts:WeatherUrl"]))
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

        // import weather forecast
        try
        {
            logger.LogInformation("Schedule.Weather :: updating weather");

            DateTime date = weatherData.Forecast.Tabular.Time.OrderBy(t => t.From).Select(t => t.From.Date).FirstOrDefault();
            var previousForecast = context.WeatherForecast.OrderBy(wf => wf.Date).ThenBy(wf => wf.Period).Where(wf => wf.Date >= date).ToList();

            foreach(var item in weatherData.Forecast.Tabular.Time)
            {
                WeatherForecast forecast = previousForecast.FirstOrDefault(f => f.Date == item.From.Date && (int)f.Period == item.Period);
                if(forecast == null)
                {
                    forecast = new WeatherForecast();
                    context.WeatherForecast.Add(forecast);
                }

                forecast.Date = item.From.Date;
                forecast.Period = (WeatherForecastPeriod)item.Period;
                forecast.SymbolID = item.Symbol.Var;

                forecast.Temperature = item.Temperature.Value;

                forecast.WindSpeed = item.WindSpeed.Mps;
                forecast.WindDirection = item.WindDirection.Code;

                forecast.Rain = item.Precipitation.Value;
            }

            context.SaveChanges();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to store weather information in database.");
        }
    }
}
