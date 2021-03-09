using HomeAutomation.Entities;
using HomeAutomation.Entities.Enums;
using HomeAutomation.Entities.Triggers;
using HomeAutomation.Services;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.ScheduledJobs
{
    [DisallowConcurrentExecution]
    public class TriggerScheduledJob : IJob
    {
        private readonly IJsonDatabaseService jsonDatabaseService;
        private readonly ISunDataService sunDataService;
        private readonly IActionExecutionService actionExecutionService;

        public TriggerScheduledJob(IJsonDatabaseService jsonDatabaseService, ISunDataService sunDataService, IActionExecutionService actionExecutionService)
        {
            this.jsonDatabaseService = jsonDatabaseService;
            this.sunDataService = sunDataService;
            this.actionExecutionService = actionExecutionService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            // get the from and to dates
            DateTime from = context.PreviousFireTimeUtc.HasValue ? context.PreviousFireTimeUtc.Value.LocalDateTime : DateTime.Now.AddMinutes(-5);
            DateTime to = context.FireTimeUtc.LocalDateTime;

            List<Trigger> triggers = new List<Trigger>();

            // calculate all the triggers
            foreach (var trigger in jsonDatabaseService.ScheduledTriggers)
            {
                DateTime calculatedTime = CalculateTriggerTime(trigger.At, trigger.Mode);
                if (calculatedTime > from && to >= calculatedTime)
                    triggers.Add(trigger);
            }

            // fire all triggers found
            foreach (var trigger in triggers)
                foreach (int action in trigger.Actions)
                    await actionExecutionService.Execute(action, trigger);
        }

        private DateTime CalculateTriggerTime(TimeSpan at, ScheduleMode mode)
        {
            var sunData = sunDataService.GetLatest();

            DateTime calculatedAt = DateTime.Today;
            
            if(mode == ScheduleMode.Sunrise)
                calculatedAt = calculatedAt.Add(sunData.Sunrise.TimeOfDay);

            if (mode == ScheduleMode.Sunset)
                calculatedAt = calculatedAt.Add(sunData.Sunset.TimeOfDay);

            // set the event time, if together with sunrise/sunset add for example 5 minutes from that time
            return calculatedAt.Add(at);
        }
    }
}
