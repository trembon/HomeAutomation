using HomeAutomation.Core.ScheduledJobs.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace HomeAutomation.Core.Extensions;

public static class ScheduledJobExtensions
{
    public static void AddScheduleJobs(this IServiceCollection services, IConfiguration configuration)
    {
        var scheduledJobConfiguration = configuration.GetSection("ScheduledJobs").Get<ScheduledJobConfiguration>() ?? new ScheduledJobConfiguration();
        var jobInterface = typeof(IScheduledJob);
        var addMethod = typeof(ScheduledJobExtensions)
            .GetMethod(nameof(AddScheduleJob), BindingFlags.Public | BindingFlags.Static)!;

        if (!scheduledJobConfiguration.Enabled)
            return;

        var enabledJobs = scheduledJobConfiguration.EnabledJobs
            .Where(static jobName => !string.IsNullOrWhiteSpace(jobName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var jobTypes = jobInterface.Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && jobInterface.IsAssignableFrom(t) && t.GetCustomAttribute<ScheduledJobAttribute>() != null)
            .Where(t => enabledJobs.Count == 0 || enabledJobs.Contains(t.Name));

        foreach (var jobType in jobTypes)
            addMethod.MakeGenericMethod(jobType).Invoke(null, [services]);
    }

    public static void AddScheduleJob<TScheduledJob>(this IServiceCollection serviceCollection) where TScheduledJob : class, IScheduledJob
    {
        serviceCollection.AddTransient<TScheduledJob>();
        serviceCollection.AddHostedService<ScheduledJobHandler<TScheduledJob>>();
    }
}
