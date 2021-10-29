using HomeAutomation.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.ScheduledJobs
{
    [DisallowConcurrentExecution]
    public class CleanupLogScheduleJob : IJob
    {
        private readonly LogContext logContext;
        private readonly ILogger<CleanupLogScheduleJob> logger;

        public CleanupLogScheduleJob(LogContext logContext, ILogger<CleanupLogScheduleJob> logger)
        {
            this.logContext = logContext;
            this.logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogInformation("Schedule.Cleanup :: starting");

            DateTime limit = DateTime.UtcNow.AddDays(-7); // TODO: place in configuration?
            var rows = await logContext.Rows.Where(x => limit > x.Timestamp).ToListAsync();
            logContext.RemoveRange(rows);
            await logContext.SaveChangesAsync();

            logger.LogInformation("Schedule.Cleanup :: done");
        }
    }
}
