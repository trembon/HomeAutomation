namespace HomeAutomation.Core.ScheduledJobs.Base;

public interface IScheduledJob
{
    Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken);
}
