using HomeAutomation.Entities.Devices;
using HomeAutomation.Entities.Enums;
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
    }

    public class TriggerService : ITriggerService
    {
        private readonly IActionExecutionService actionExecutionService;
        private readonly IJsonDatabaseService memoryEntitiesService;
        private readonly ILogger<TriggerService> logger;

        public TriggerService(IActionExecutionService actionExecutionService, IJsonDatabaseService memoryEntitiesService, ILogger<TriggerService> logger)
        {
            this.actionExecutionService = actionExecutionService;
            this.memoryEntitiesService = memoryEntitiesService;
            this.logger = logger;
        }

        public async Task FireTriggersFromDevice(Device device, DeviceEvent deviceEvent)
        {
            logger.LogInformation($"Firing triggers from device '{device}' and event '{deviceEvent}'");

            var triggers = memoryEntitiesService.StateTriggers.Where(st => st.Events.Contains(deviceEvent) && st.Devices.Contains(device.ID));
            foreach(var trigger in triggers)
                foreach(var action in trigger.Actions)
                    await actionExecutionService.Execute(action, device);
        }
    }
}
