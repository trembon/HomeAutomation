using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Core.Services;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;

namespace HomeAutomation.Core.ScheduledJobs;

public class TriggerScheduledJob(ITriggerRepository repository, ISunDataService sunDataService, ITriggerService triggerService) : IScheduledJob
{
    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        // get the from and to dates
        DateTime from = lastExecution.HasValue ? lastExecution.Value.ToLocalTime() : DateTime.Now.AddMinutes(-5);
        DateTime to = currentExecution.ToLocalTime();

        List<TriggerEntity> triggers = [];

        // calculate all the triggers
        foreach (var trigger in await repository.GetScheduledTriggers(cancellationToken))
        {
            if (trigger.ScheduledAt is null || trigger.SchedulingMode is null)
                continue;

            DateTime calculatedTime = CalculateTriggerTime(trigger.ScheduledAt.Value, trigger.SchedulingMode.Value);
            if (calculatedTime > from && to >= calculatedTime)
                triggers.Add(trigger);
        }

        // fire all found triggers
        if (triggers.Count > 0)
            await triggerService.FireTriggers(triggers, cancellationToken);
    }

    private DateTime CalculateTriggerTime(TimeOnly at, TimeMode mode)
    {
        var sunData = sunDataService.GetLatest();

        DateTime calculatedAt = DateTime.Today;

        if (mode == TimeMode.Sunrise)
            calculatedAt = calculatedAt.Add(sunData.Sunrise.ToTimeSpan());

        if (mode == TimeMode.Sunset)
            calculatedAt = calculatedAt.Add(sunData.Sunset.ToTimeSpan());

        // set the event time, if together with sunrise/sunset add for example 5 minutes from that time
        return calculatedAt.Add(at.ToTimeSpan());
    }
}
