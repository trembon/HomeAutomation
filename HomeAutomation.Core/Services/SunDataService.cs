using HomeAutomation.Database;
using HomeAutomation.Database.Entities;
using Microsoft.Extensions.Logging;

namespace HomeAutomation.Core.Services;

public interface ISunDataService
{
    SunDataEntity GetLatest();

    bool Add(DateOnly date, TimeOnly sunrise, TimeOnly sunset);
}

public class SunDataService(DefaultContext context, ILogger<SunDataService> logger) : ISunDataService
{
    private static SunDataEntity? latestCache;
    private static readonly Lock latestCacheLock = new();

    public bool Add(DateOnly date, TimeOnly sunrise, TimeOnly sunset)
    {
        lock (latestCacheLock)
        {
            if (context.SunData.Any(sd => sd.Date == date))
                return false;

            logger.LogInformation("Updating sun information.");

            try
            {
                SunDataEntity sunData = new()
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

    public SunDataEntity GetLatest()
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
                return new SunDataEntity { Id = 0, Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), Sunrise = new TimeOnly(8, 0), Sunset = new TimeOnly(20, 0) };

            return latestCache;
        }
    }
}
