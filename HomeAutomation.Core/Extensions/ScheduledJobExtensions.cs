using HomeAutomation.Core.ScheduledJobs.Base;
using Microsoft.Extensions.DependencyInjection;

namespace HomeAutomation.Core.Extensions;

public static class ScheduledJobExtensions
{
    public static void AddScheduleJob<TScheduledJob>(this IServiceCollection serviceCollection) where TScheduledJob : class, IScheduledJob
    {
        serviceCollection.AddTransient<TScheduledJob>();
        serviceCollection.AddHostedService<ScheduledJobHandler<TScheduledJob>>();
    }
}
