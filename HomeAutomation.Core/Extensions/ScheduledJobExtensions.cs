using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.ScheduledJobs;
using Microsoft.Extensions.DependencyInjection;

namespace HomeAutomation.Core.Extensions;

public static class ScheduledJobExtensions
{
    public static void AddScheduleJob<TScheduledJob>(this IServiceCollection serviceCollection) where TScheduledJob : IScheduledJob
    {
        serviceCollection.AddHostedService<ScheduledJobHandler<TScheduledJob>>();
        serviceCollection.AddTransient<CleanupLogScheduleJob>();
    }
}
