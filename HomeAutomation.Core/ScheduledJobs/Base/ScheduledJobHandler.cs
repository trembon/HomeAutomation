using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HomeAutomation.Core.ScheduledJobs.Base;

public class ScheduledJobHandler<TScheduledJob>(IServiceProvider serviceProvider, IConfiguration configuration) : IHostedService, IDisposable where TScheduledJob : IScheduledJob
{
    private Timer? _timer = null;
    private DateTime? _lastExecution = null;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        bool gotInterval = int.TryParse(configuration[$"ScheduledJobs:{typeof(TScheduledJob).Name}"], out int interval);
        if (gotInterval)
            _timer = new Timer(ProcessTimer, cancellationToken, TimeSpan.Zero, TimeSpan.FromSeconds(interval));

        return Task.CompletedTask;
    }

    private void ProcessTimer(object? state)
    {
        CancellationToken cancellationToken = state is not null ? (CancellationToken)state : CancellationToken.None;
        DateTime currentExecution = DateTime.UtcNow;

        using var scope = serviceProvider.CreateScope();
        var scheduledJob = scope.ServiceProvider.GetRequiredService<TScheduledJob>();
        scheduledJob.Execute(currentExecution, _lastExecution, cancellationToken).Wait();

        _lastExecution = currentExecution;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}
