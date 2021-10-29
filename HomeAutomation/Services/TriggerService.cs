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

        public async Task FireTriggersFromDevice(Device device, DeviceEvent deviceEvent)
        {
            var triggers = memoryEntitiesService.StateTriggers.Where(st => st.Events.Contains(deviceEvent) && st.Devices.Contains(device.ID));

            if (triggers != null && triggers.Any())
            {
                logger.LogInformation($"Firing triggers ({string.Join(',', triggers.Select(x => x.ID))}) from device '{device}' and event '{deviceEvent}'");
                await FireTriggers(triggers, device);
            }
            else
            {
                logger.LogInformation($"No triggers to fire device '{device}' and event '{deviceEvent}'");
            }
        }

        public Task FireTriggers(IEnumerable<Trigger> triggers)
        {
            return FireTriggers(triggers, null);
        }

        public async Task FireTriggers(IEnumerable<Trigger> triggers, IEntity source)
        {
            if (triggers == null || !triggers.Any())
                return;

            // fire all triggers found
            foreach (var trigger in triggers)
            {
                if (trigger.Disabled)
                {
                    logger.LogInformation($"Trigger with ID {trigger.ID} is disabled");
                    continue;
                }

                bool meetConditions = await evaluateConditionService.MeetConditions(trigger, trigger.Conditions);
                if (!meetConditions)
                {
                    logger.LogInformation($"Trigger with ID {trigger.ID} didnt meet the configured conditions");
                    continue;
                }

                foreach (int action in trigger.Actions)
                {
                    await actionExecutionService.Execute(action, source ?? trigger);
                }
            }
        }
    }
}
