﻿using EFCore.BulkExtensions;
using HomeAutomation.Database;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
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

            DateTime loglimit = DateTime.UtcNow.AddDays(-7); // TODO: place in configuration?
            _ = await logContext.Rows.Where(x => loglimit > x.Timestamp).BatchDeleteAsync();


            DateTime maillimit = DateTime.UtcNow.AddDays(-3); // TODO: place in configuration?
            _ = await logContext.MailMessages.Where(x => maillimit > x.Timestamp).BatchDeleteAsync();

            logger.LogInformation("Schedule.Cleanup :: done");
        }
    }
}
