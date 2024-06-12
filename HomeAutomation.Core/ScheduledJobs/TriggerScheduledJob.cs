using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Core.Services;
using HomeAutomation.Entities.Enums;
using HomeAutomation.Entities.Triggers;

namespace HomeAutomation.ScheduledJobs;

public class TriggerScheduledJob(IJsonDatabaseService jsonDatabaseService, ISunDataService sunDataService, ITriggerService triggerService) : IScheduledJob
{
    public Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        // get the from and to dates
        DateTime from = lastExecution.HasValue ? lastExecution.Value.ToLocalTime() : DateTime.Now.AddMinutes(-5);
        DateTime to = currentExecution.ToLocalTime();

        List<Trigger> triggers = [];

        // calculate all the triggers
        foreach (var trigger in jsonDatabaseService.ScheduledTriggers)
        {
            DateTime calculatedTime = CalculateTriggerTime(trigger.At, trigger.Mode);
            if (calculatedTime > from && to >= calculatedTime)
                triggers.Add(trigger);
        }

        // fire all found triggers
        if (triggers.Any())
            return triggerService.FireTriggers(triggers);

        return Task.CompletedTask;
    }

    private DateTime CalculateTriggerTime(TimeSpan at, ScheduleMode mode)
    {
        var sunData = sunDataService.GetLatest();

        DateTime calculatedAt = DateTime.Today;
        
        if(mode == ScheduleMode.Sunrise)
            calculatedAt = calculatedAt.Add(sunData.Sunrise.ToTimeSpan());

        if (mode == ScheduleMode.Sunset)
            calculatedAt = calculatedAt.Add(sunData.Sunset.ToTimeSpan());

        // set the event time, if together with sunrise/sunset add for example 5 minutes from that time
        return calculatedAt.Add(at);
    }
}
