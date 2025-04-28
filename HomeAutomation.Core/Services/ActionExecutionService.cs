using HomeAutomation.Core.Entities;
using HomeAutomation.Database.Entities;
using Microsoft.Extensions.Logging;

namespace HomeAutomation.Core.Services;

public interface IActionExecutionService
{
    Task Execute(int actionId, object? source, CancellationToken cancellationToken);
}

public class ActionExecutionService(IDeviceService deviceService, IEvaluateConditionService evaluateConditionService, IServiceProvider serviceProvider, ILogger<ActionExecutionService> logger) : IActionExecutionService
{
    public async Task Execute(int actionId, object? source, CancellationToken cancellationToken)
    {
        var action = memoryEntitiesService.Actions.FirstOrDefault(a => a.ID == actionId);
        if (action == null)
        {
            logger.LogError("Action.Execute :: {actionId} :: Status:NotFound", actionId);
            return;
        }

        if (action.Disabled)
        {
            logger.LogInformation("Action.Execute :: {actionId} :: Status:Disabled", actionId);
            return;
        }

        bool meetConditions = await evaluateConditionService.MeetConditions(action, action.Conditions);
        if (!meetConditions)
        {
            logger.LogInformation("Action.Execute :: {actionId} :: Status:ConditionsNotMet", actionId);
            return;
        }

        List<DeviceEntity> devices = new(action.Devices?.Length ?? 0);
        if (action.Devices != null)
        {
            foreach (int deviceId in action.Devices)
            {
                var device = await deviceService.GetDevice(deviceId, cancellationToken);
                if (device is not null)
                    devices.Add(device);
            }
        }

        logger.LogInformation("Action.Execute :: {actionId} :: Source:{source}, Devices:{deviceIds}", actionId, source, string.Join(',', devices.Select(x => x.Id)));

        try
        {
            var arguments = new ActionExecutionArguments(source, devices, serviceProvider);
            await action.Execute(arguments);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Action.Execute :: {actionId} :: Error:{message}", actionId, ex.Message);
        }
    }
}
