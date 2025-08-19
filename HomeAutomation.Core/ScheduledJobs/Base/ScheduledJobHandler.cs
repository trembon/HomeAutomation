using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeAutomation.Core.ScheduledJobs.Base;

public class ScheduledJobHandler<TScheduledJob>(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration, ILogger<ScheduledJobHandler<TScheduledJob>> logger) : IHostedService, IDisposable where TScheduledJob : IScheduledJob
{
    private Timer? _timer = null;
    private DateTime? _lastExecution = null;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        bool gotInterval = int.TryParse(configuration[$"ScheduledJobs:{typeof(TScheduledJob).Name}"], out int interval);
        if (gotInterval)
        {
            _timer = new Timer(ProcessTimer, cancellationToken, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(interval));
            logger.LogInformation("Starting scheduled job for {class} with interval {interval}", typeof(TScheduledJob).Name, interval);
        }
        else
        {
            logger.LogInformation("Skipping scheduled job for {class}, not configured interval found", typeof(TScheduledJob).Name);
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
