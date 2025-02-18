using HomeAutomation.Database;
using HomeAutomation.Database.Contexts;
using HomeAutomation.Database.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Core.Services;

public interface ISunDataService
{
    SunData GetLatest();

    bool Add(DateOnly date, TimeOnly sunrise, TimeOnly sunset);
}

public class SunDataService : ISunDataService
{
    private readonly ILogger<SunDataService> logger;

    private readonly DefaultContext context;

    private static SunData latestCache;
    private static object latestCacheLock = new object();

    public SunDataService(DefaultContext context, ILogger<SunDataService> logger)
    {
        this.logger = logger;

        this.context = context;
    }

    public bool Add(DateOnly date, TimeOnly sunrise, TimeOnly sunset)
    {
        lock (latestCacheLock)
        {
            if (context.SunData.Any(sd => sd.Date == date))
                return false;

            logger.LogInformation("Updating sun information.");

            try
            {
                SunData sunData = new()
                {
                    Date = date,
                    Sunrise = sunrise,
                    Sunset = sunset
                };

                context.Add(sunData);
                bool result = context.SaveChanges() > 0;

                if (latestCache == null || (result && sunData.Date > latestCache.Date))
                    latestCache = sunData;

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to store sun information in database ({date.ToShortDateString()}).");
                return false;
            }
        }
    }

    public SunData GetLatest()
    {
        lock (latestCacheLock)
        {
            if (latestCache == null)
            {
                try
                {
                    latestCache = context.SunData.OrderByDescending(sd => sd.Date).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to fetch sun information from database.");
                }
            }

            if (latestCache == null)
                return new SunData { ID = 0, Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), Sunrise = new TimeOnly(8, 0), Sunset = new TimeOnly(20, 0) };

            return latestCache;
        }
    }
}
