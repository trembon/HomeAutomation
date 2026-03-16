using HomeAutomation.Core.ScheduledJobs.Base;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace HomeAutomation.Core.Extensions;

public static class ScheduledJobExtensions
{
    public static void AddScheduleJobs(this IServiceCollection services)
    {
        var jobInterface = typeof(IScheduledJob);
        var addMethod = typeof(ScheduledJobExtensions)
            .GetMethod(nameof(AddScheduleJob), BindingFlags.Public | BindingFlags.Static)!;

        var jobTypes = jobInterface.Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && jobInterface.IsAssignableFrom(t) && t.GetCustomAttribute<ScheduledJobAttribute>() != null);

        foreach (var jobType in jobTypes)
            addMethod.MakeGenericMethod(jobType).Invoke(null, [services]);
    }

    public static void AddScheduleJob<TScheduledJob>(this IServiceCollection serviceCollection) where TScheduledJob : class, IScheduledJob
    {
        serviceCollection.AddTransient<TScheduledJob>();
        serviceCollection.AddHostedService<ScheduledJobHandler<TScheduledJob>>();
    }
}
