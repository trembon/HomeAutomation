using HomeAutomation.Entities;
using HomeAutomation.Entities.Devices;
using HomeAutomation.Models.Actions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Services
{
    public interface IActionExecutionService
    {
        Task Execute(int actionId, IEntity source);
    }

    public class ActionExecutionService : IActionExecutionService
    {
        private readonly IJsonDatabaseService memoryEntitiesService;
        private readonly IEvaluateConditionService evaluateConditionService;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<ActionExecutionService> logger;

        public ActionExecutionService(IJsonDatabaseService memoryEntitiesService, IEvaluateConditionService evaluateConditionService, IServiceProvider serviceProvider, ILogger<ActionExecutionService> logger)
        {
            this.memoryEntitiesService = memoryEntitiesService;
            this.evaluateConditionService = evaluateConditionService;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        public async Task Execute(int actionId, IEntity source)
        {
            logger.LogInformation($"Starting action with ID {actionId} from source '{source}'");

            var action = memoryEntitiesService.Actions.FirstOrDefault(a => a.ID == actionId);
            if (action == null)
            {
                logger.LogError($"Action with ID {actionId} was not found");
                return;
            }

            if (action.Disabled)
            {
                logger.LogInformation($"Action with ID {actionId} is disabled");
                return;
            }

            bool meetConditions = await evaluateConditionService.MeetConditions(action, action.Conditions);
            if (!meetConditions)
            {
                logger.LogInformation($"Action with ID {actionId} didnt meet the configured conditions");
                return;
            }

            var devices = new List<Device>(action.Devices?.Length ?? 0);
            if (action.Devices != null)
            {
                foreach (int deviceId in action.Devices)
                {
                    var device = memoryEntitiesService.Devices.FirstOrDefault(d => d.ID == deviceId);
                    devices.Add(device);
                }
            }

            var arguments = new ActionExecutionArguments(source, devices, serviceProvider);
            await action.Execute(arguments);
        }
    }
}
