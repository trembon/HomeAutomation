using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeAutomation.ScheduledJobs;

public class CleanupLogScheduleJob(LogContext logContext, ILogger<CleanupLogScheduleJob> logger) : IScheduledJob
{
    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        logger.LogInformation("Schedule.Cleanup :: starting");

        DateTime loglimit = DateTime.UtcNow.AddDays(-7); // TODO: place in configuration?
        _ = await logContext.Rows.Where(x => loglimit > x.Timestamp).ExecuteDeleteAsync();


        DateTime maillimit = DateTime.UtcNow.AddDays(-3); // TODO: place in configuration?
        _ = await logContext.MailMessages.Where(x => maillimit > x.Timestamp).ExecuteDeleteAsync();

        logger.LogInformation("Schedule.Cleanup :: done");
    }
}
