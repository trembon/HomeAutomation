using HomeAutomation.Database.Entities;
using HomeAutomation.Entities.Enums;
using HomeAutomation.Entities.Triggers;
using Microsoft.Extensions.Logging;

namespace HomeAutomation.Core.Services;

public interface ITriggerService
{
    Task FireTriggersFromDevice(Device device, DeviceEvent deviceEvent);

    Task FireTriggers(IEnumerable<Trigger> triggers);

    Task FireTriggers(IEnumerable<Trigger> triggers, object? source);
}

public class TriggerService : ITriggerService
{
    private readonly IActionExecutionService actionExecutionService;
    private readonly IEvaluateConditionService evaluateConditionService;
    private readonly IJsonDatabaseService memoryEntitiesService;
    private readonly ILogger<TriggerService> logger;

    public TriggerService(IActionExecutionService actionExecutionService, IEvaluateConditionService evaluateConditionService, IJsonDatabaseService memoryEntitiesService, ILogger<TriggerService> logger)
    {
        this.actionExecutionService = actionExecutionService;
        this.evaluateConditionService = evaluateConditionService;
        this.memoryEntitiesService = memoryEntitiesService;
        this.logger = logger;
    }

    public Task FireTriggersFromDevice(Device device, DeviceEvent deviceEvent)
    {
        // ignore events that are not known
        if (deviceEvent == DeviceEvent.Unknown)
            return Task.CompletedTask;

        var triggers = memoryEntitiesService.StateTriggers.Where(st => st.Events.Contains(deviceEvent) && st.Devices.Contains(device.Id));

        if (triggers != null && triggers.Any())
        {
            logger.LogInformation($"Triggers.FireAll :: {string.Join(',', triggers.Select(x => x.ID))} :: Device:{device.Id}, Event:{deviceEvent}");
            return ExecuteTriggerActions(triggers, device);
        }
        else
        {
            logger.LogInformation($"Triggers.FireAll :: None :: Device:{device.Id}, Event:{deviceEvent}");
            return Task.CompletedTask;
        }
    }

    public Task FireTriggers(IEnumerable<Trigger> triggers)
    {
        if (triggers != null && triggers.Any())
        {
            logger.LogInformation("Triggers.FireAll :: {triggers}", string.Join(',', triggers.Select(x => x.ID)));
            return ExecuteTriggerActions(triggers, null);
        }
        else
        {
            logger.LogInformation($"Triggers.FireAll :: None");
            return Task.CompletedTask;
        }
    }

    public Task FireTriggers(IEnumerable<Trigger> triggers, object? source)
    {
        if (triggers != null && triggers.Any())
        {
            logger.LogInformation("Triggers.FireAll :: {triggers} :: Source:{source}", string.Join(',', triggers.Select(x => x.ID)), source);
            return ExecuteTriggerActions(triggers, source);
        }
        else
        {
            logger.LogInformation("Triggers.FireAll :: None :: Source:{source}", source);
            return Task.CompletedTask;
        }
    }

    private async Task ExecuteTriggerActions(IEnumerable<Trigger> triggers, object? source)
    {
        foreach (var trigger in triggers)
        {
            if (trigger.Disabled)
            {
                logger.LogInformation("Trigger.Fire :: {triggerId} :: Status:Disabled", trigger.ID);
                continue;
            }

            bool meetConditions = await evaluateConditionService.MeetConditions(trigger, trigger.Conditions);
            if (!meetConditions)
            {
                logger.LogInformation("Trigger.Fire :: {triggerId} :: Status:ConditionsNotMet", trigger.ID);
                continue;
            }

            logger.LogInformation("Trigger.Fire :: {triggerId}", trigger.ID);

            List<Task> triggerActionTasks = new(trigger.Actions.Length);
            foreach (int action in trigger.Actions)
            {
                triggerActionTasks.Add(actionExecutionService.Execute(action, source ?? trigger));
            }

            await Task.WhenAll(triggerActionTasks);
        }
    }
}
