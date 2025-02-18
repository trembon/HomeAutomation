using HomeAutomation.Core.Entities;
using HomeAutomation.Entities;
using HomeAutomation.Entities.Devices;
using Microsoft.Extensions.Logging;

namespace HomeAutomation.Core.Services;

public interface IActionExecutionService
{
    Task Execute(int actionId, IEntity source);
}

public class ActionExecutionService(IJsonDatabaseService memoryEntitiesService, IEvaluateConditionService evaluateConditionService, IServiceProvider serviceProvider, ILogger<ActionExecutionService> logger) : IActionExecutionService
{
    public async Task Execute(int actionId, IEntity source)
    {
        var action = memoryEntitiesService.Actions.FirstOrDefault(a => a.ID == actionId);
        if (action == null)
        {
            logger.LogError($"Action.Execute :: {actionId} :: Status:NotFound");
            return;
        }

        if (action.Disabled)
        {
            logger.LogInformation($"Action.Execute :: {actionId} :: Status:Disabled");
            return;
        }

        bool meetConditions = await evaluateConditionService.MeetConditions(action, action.Conditions);
        if (!meetConditions)
        {
            logger.LogInformation($"Action.Execute :: {actionId} :: Status:ConditionsNotMet");
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

        logger.LogInformation($"Action.Execute :: {actionId} :: Source:{source.ToSourceString()}, Devices:{string.Join(',', devices.Select(x => x.ID))}");

        try
        {
            var arguments = new ActionExecutionArguments(source, devices, serviceProvider);
            await action.Execute(arguments);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Action.Execute :: {actionId} :: Error:{ex.Message}");
        }
    }
}
