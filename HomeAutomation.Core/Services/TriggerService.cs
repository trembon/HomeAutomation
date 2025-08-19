using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeAutomation.Core.Services;

public interface ITriggerService
{
    Task FireTriggersFromDevice(DeviceEntity device, DeviceEvent deviceEvent, CancellationToken cancellationToken);

    Task FireTriggers(IEnumerable<TriggerEntity> triggers, CancellationToken cancellationToken);

    Task FireTriggers(IEnumerable<TriggerEntity> triggers, object? source, CancellationToken cancellationToken);
}

public class TriggerService(ITriggerRepository repository, IActionExecutionService actionExecutionService, IEvaluateConditionService evaluateConditionService, ILogger<TriggerService> logger) : ITriggerService
{
    public async Task FireTriggersFromDevice(DeviceEntity device, DeviceEvent deviceEvent, CancellationToken cancellationToken)
    {
        // ignore events that are not known
        if (deviceEvent == DeviceEvent.Unknown)
            return;

        var triggers = await repository
            .Table
            .Include(x => x.Conditions)
            .Where(x => x.Kind == TriggerKind.DeviceState && x.ListenOnDeviceId == device.Id && x.ListenOnDeviceEvent == deviceEvent)
            .ToListAsync(cancellationToken);

        if (triggers.Count > 0)
        {
            logger.LogInformation("Triggers.FireAll :: {triggers} :: Device:{deviceId}, Event:{deviceEvent}", string.Join(',', triggers.Select(x => x.Id)), device.Id, deviceEvent);
            await ExecuteTriggerActions(triggers, device, cancellationToken);
        }
        else
        {
            logger.LogInformation("Triggers.FireAll :: None :: Device:{deviceId}, Event:{deviceEvent}", device.Id, deviceEvent);
        }
    }

    public async Task FireTriggers(IEnumerable<TriggerEntity> triggers, CancellationToken cancellationToken)
    {
        if (triggers != null && triggers.Any())
        {
            logger.LogInformation("Triggers.FireAll :: {triggers}", string.Join(',', triggers.Select(x => x.Id)));
            await ExecuteTriggerActions(triggers, null, cancellationToken);
        }
        else
        {
            logger.LogInformation($"Triggers.FireAll :: None");
        }
    }

    public async Task FireTriggers(IEnumerable<TriggerEntity> triggers, object? source, CancellationToken cancellationToken)
    {
        if (triggers != null && triggers.Any())
        {
            logger.LogInformation("Triggers.FireAll :: {triggers} :: Source:{source}", string.Join(',', triggers.Select(x => x.Id)), source);
            await ExecuteTriggerActions(triggers, source, cancellationToken);
        }
        else
        {
            logger.LogInformation("Triggers.FireAll :: None :: Source:{source}", source);
        }
    }

    private async Task ExecuteTriggerActions(IEnumerable<TriggerEntity> triggers, object? source, CancellationToken cancellationToken)
    {
        foreach (var trigger in triggers)
        {
            if (trigger.Disabled)
            {
                logger.LogInformation("Trigger.Fire :: {triggerId} :: Status:Disabled", trigger.Id);
                continue;
            }

            bool meetConditions = evaluateConditionService.MeetConditions(trigger);
            if (!meetConditions)
            {
                logger.LogInformation("Trigger.Fire :: {triggerId} :: Status:ConditionsNotMet", trigger.Id);
                continue;
            }

            logger.LogInformation("Trigger.Fire :: {triggerId}", trigger.Id);

            var actions = await repository.GetActionsForTrigger(trigger.Id, cancellationToken);
            foreach (var action in actions)
                await actionExecutionService.Execute(action.Id, source ?? trigger, cancellationToken);
        }
    }
}
