using HomeAutomation.Entities;
using HomeAutomation.Entities.Devices;
using HomeAutomation.Entities.Enums;
using HomeAutomation.Entities.Triggers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Services
{
    public interface ITriggerService
    {
        Task FireTriggersFromDevice(Device device, DeviceEvent state);

        Task FireTriggers(IEnumerable<Trigger> triggers);

        Task FireTriggers(IEnumerable<Trigger> triggers, IEntity source);
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

            var triggers = memoryEntitiesService.StateTriggers.Where(st => st.Events.Contains(deviceEvent) && st.Devices.Contains(device.ID));

            if (triggers != null && triggers.Any())
            {
                logger.LogInformation($"Triggers.FireAll :: {string.Join(',', triggers.Select(x => x.ID))} :: Device:{device.ID}, Event:{deviceEvent}");
                return ExecuteTriggerActions(triggers, device);
            }
            else
            {
                logger.LogInformation($"Triggers.FireAll :: None :: Device:{device.ID}, Event:{deviceEvent}");
                return Task.CompletedTask;
            }
        }

        public Task FireTriggers(IEnumerable<Trigger> triggers)
        {
            if (triggers != null && triggers.Any())
            {
                logger.LogInformation($"Triggers.FireAll :: {string.Join(',', triggers.Select(x => x.ID))}");
                return ExecuteTriggerActions(triggers, null);
            }
            else
            {
                logger.LogInformation($"Triggers.FireAll :: None");
                return Task.CompletedTask;
            }
        }

        public Task FireTriggers(IEnumerable<Trigger> triggers, IEntity source)
        {
            if (triggers != null && triggers.Any())
            {
                logger.LogInformation($"Triggers.FireAll :: {string.Join(',', triggers.Select(x => x.ID))} :: Source:{source.ToSourceString()}");
                return ExecuteTriggerActions(triggers, source);
            }
            else
            {
                logger.LogInformation($"Triggers.FireAll :: None :: Source:{source.ToSourceString()}");
                return Task.CompletedTask;
            }
        }

        private async Task ExecuteTriggerActions(IEnumerable<Trigger> triggers, IEntity source)
        {
            foreach (var trigger in triggers)
            {
                if (trigger.Disabled)
                {
                    logger.LogInformation($"Trigger.Fire :: {trigger.ID} :: Status:Disabled");
                    continue;
                }

                bool meetConditions = await evaluateConditionService.MeetConditions(trigger, trigger.Conditions);
                if (!meetConditions)
                {
                    logger.LogInformation($"Trigger.Fire :: {trigger.ID} :: Status:ConditionsNotMet");
                    continue;
                }

                logger.LogInformation($"Trigger.Fire :: {trigger.ID}");

                List<Task> triggerActionTasks = new(trigger.Actions.Length);
                foreach (int action in trigger.Actions)
                {
                    triggerActionTasks.Add(actionExecutionService.Execute(action, source ?? trigger));
                }

                await Task.WhenAll(triggerActionTasks);
            }
        }
    }
}
