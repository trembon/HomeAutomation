using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeAutomation.Core.ScheduledJobs;

[ScheduledJob(86400)]
public class CleanupLogScheduleJob(DefaultContext context, ILogger<CleanupLogScheduleJob> logger) : IScheduledJob
{
    private const int KEEP_LOGS_DAYS = 7;
    private const int KEEP_MAILS_DAYS = 3;

    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        logger.LogInformation("Schedule.Cleanup :: starting");

        DateTime loglimit = DateTime.UtcNow.AddDays(-KEEP_LOGS_DAYS);
        _ = await context.Logs.Where(x => loglimit > x.Timestamp).ExecuteDeleteAsync(cancellationToken);

        DateTime maillimit = DateTime.UtcNow.AddDays(-KEEP_MAILS_DAYS);
        _ = await context.MailMessages.Where(x => maillimit > x.Timestamp).ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation("Schedule.Cleanup :: done");
    }
}
