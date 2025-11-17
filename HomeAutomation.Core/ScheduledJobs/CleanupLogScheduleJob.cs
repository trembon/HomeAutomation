using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeAutomation.Core.ScheduledJobs;

public class CleanupLogScheduleJob(DefaultContext context, ILogger<CleanupLogScheduleJob> logger) : IScheduledJob
{
    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        logger.LogInformation("Schedule.Cleanup :: starting");

        DateTime loglimit = DateTime.UtcNow.AddDays(-7); // TODO: place in configuration?
        _ = await context.Logs.Where(x => loglimit > x.Timestamp).ExecuteDeleteAsync();

        DateTime maillimit = DateTime.UtcNow.AddDays(-3); // TODO: place in configuration?
        _ = await context.MailMessages.Where(x => maillimit > x.Timestamp).ExecuteDeleteAsync();

        logger.LogInformation("Schedule.Cleanup :: done");
    }
}
