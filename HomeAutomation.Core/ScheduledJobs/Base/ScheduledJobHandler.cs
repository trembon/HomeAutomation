using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace HomeAutomation.Core.ScheduledJobs.Base;

public class ScheduledJobHandler<TScheduledJob>(IServiceScopeFactory serviceScopeFactory, ILogger<ScheduledJobHandler<TScheduledJob>> logger) : IHostedService, IDisposable where TScheduledJob : IScheduledJob
{
    private Timer? _timer = null;
    private DateTime? _lastExecution = null;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var attribute = typeof(TScheduledJob).GetCustomAttribute<ScheduledJobAttribute>();
        if (attribute != null)
        {
            _timer = new Timer(ProcessTimer, cancellationToken, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(attribute.IntervalInSeconds));
            logger.LogInformation("Starting scheduled job for {class} with interval {interval}s", typeof(TScheduledJob).Name, attribute.IntervalInSeconds);
        }
        else
        {
            logger.LogWarning("Skipping scheduled job for {class}, no ScheduledJobAttribute found", typeof(TScheduledJob).Name);
        }

        return Task.CompletedTask;
    }

    private void ProcessTimer(object? state)
    {
        CancellationToken cancellationToken = state is not null ? (CancellationToken)state : CancellationToken.None;
        if (cancellationToken.IsCancellationRequested)
            return;

        DateTime currentExecution = DateTime.Now;

        using var scope = serviceScopeFactory.CreateScope();
        try
        {
            var scheduledJob = scope.ServiceProvider.GetRequiredService<TScheduledJob>();
            scheduledJob.Execute(currentExecution, _lastExecution, cancellationToken).Wait();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scheduled job threw exception during execution");
        }

        _lastExecution = currentExecution;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}
