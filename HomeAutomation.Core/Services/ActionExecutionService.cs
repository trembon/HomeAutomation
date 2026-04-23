using HomeAutomation.Base.Enums;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HomeAutomation.Core.Services;

public interface IActionExecutionService
{
    Task Execute(int actionId, object? source, CancellationToken cancellationToken);

    Task ExecuteManual(int actionId, CancellationToken cancellationToken);
}

public class ActionExecutionService(IRepository<ActionEntity> actionRepository, IDeviceRepository deviceRepository, IEvaluateConditionService evaluateConditionService, IServiceProvider serviceProvider, ILogger<ActionExecutionService> logger) : IActionExecutionService
{
    public async Task Execute(int actionId, object? source, CancellationToken cancellationToken)
    {
        var action = await actionRepository.Get(actionId, cancellationToken);
        if (action == null)
        {
            logger.LogError("Action.Execute :: {actionId} :: Status:NotFound", actionId);
            return;
        }

        if (action.Disabled)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Action.Execute :: {action} :: Status:Disabled", action.Name);
            return;
        }

        var (conditionIsMet, conditionName) = evaluateConditionService.MeetConditions(action);
        if (!conditionIsMet)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Action.Execute :: {action} :: Status:ConditionsNotMet :: Condition:{condition}", action.Name, conditionName);
            return;
        }

        List<DeviceEntity> devices = await deviceRepository.GetForAction(actionId, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Action.Execute :: {action} :: Source:{source}, Devices:{deviceIds}", action.Name, source, string.Join(',', devices.Select(x => x.Id)));

        try
        {
            await ExecuteAction(action, source, devices, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Action.Execute :: {actionId} :: Error:{message}", actionId, ex.Message);
        }
    }

    public async Task ExecuteManual(int actionId, CancellationToken cancellationToken)
    {
        var action = await actionRepository.Get(actionId, cancellationToken);
        if (action == null)
        {
            logger.LogError("Action.ExecuteManual :: {actionId} :: Status:NotFound", actionId);
            return;
        }

        List<DeviceEntity> devices = await deviceRepository.GetForAction(actionId, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Action.ExecuteManual :: {action} :: Devices:{deviceIds}", action.Name, string.Join(',', devices.Select(x => x.Id)));

        try
        {
            await ExecuteAction(action, "Manual trigger", devices, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Action.ExecuteManual :: {actionId} :: Error:{message}", actionId, ex.Message);
            throw;
        }
    }

    private Task ExecuteAction(ActionEntity action, object? source, List<DeviceEntity> devices, CancellationToken cancellationToken)
    {
        switch (action.Kind)
        {
            case ActionKind.SendMessage: return ExecuteSendMessageAction(action, source, devices, cancellationToken);
            case ActionKind.SendSnapshot: return ExecuteSendSnapshotAction(action, source, devices, cancellationToken);
            case ActionKind.DeviceEvent: return ExecuteDeviceEventAction(action, devices);

            default:
                logger.LogError("No action is defined for action kind {kind}", action.Kind);
                break;
        }

        return Task.CompletedTask;
    }

    private async Task ExecuteSendMessageAction(ActionEntity action, object? source, List<DeviceEntity> devices, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(action.MessageToSend) || string.IsNullOrWhiteSpace(action.MessageChannel))
            return;

        string message = GetMessageToSend(action, source, devices);

        var notificationService = serviceProvider.GetRequiredService<INotificationService>();
        await notificationService.SendToSlack(action.MessageChannel, message, cancellationToken);
    }

    private async Task ExecuteSendSnapshotAction(ActionEntity action, object? source, List<DeviceEntity> devices, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(action.MessageChannel))
            return;

        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var notificationService = serviceProvider.GetRequiredService<INotificationService>();

        var client = httpClientFactory.CreateClient(nameof(ActionExecutionService));

        foreach (var device in devices.Where(x => x.Kind == Database.Enums.DeviceKind.Camera))
        {
            try
            {
                string message = GetMessageToSend(action, source, devices);

                byte[] imageBytes = await client.GetByteArrayAsync(device.Url, cancellationToken);
                await notificationService.SendToSlack(action.MessageChannel, message, imageBytes, $"snapshot_{DateTime.Now:yyyy-MM-dd HHmm}.jpg", "jpg", cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to take snapshot for device ID {deviceId}.", device.Id);
            }
        }
    }

    private async Task ExecuteDeviceEventAction(ActionEntity action, List<DeviceEntity> devices)
    {
        List<Task> sendCommandTasks = [];
        foreach (var device in devices)
        {
            if (device.Source == DeviceSource.Telldus)
            {
                var telldusAPIService = serviceProvider.GetRequiredService<ITelldusAPIService>();
                var command = telldusAPIService.ConvertStateToCommand(action.DeviceEventToSend ?? DeviceEvent.Unknown);

                if (command.HasValue)
                {
                    var task = telldusAPIService.SendCommand(int.Parse(device.SourceId), command.Value);
                    sendCommandTasks.Add(task);
                }
            }

            if (device.Source == DeviceSource.ZWave)
            {
                var zwaveAPIService = serviceProvider.GetRequiredService<IZWaveAPIService>();
                ZWaveCommandClass? command = zwaveAPIService.ConvertStateToCommand(action.DeviceEventToSend ?? DeviceEvent.Unknown, out object parameter);

                if (command.HasValue)
                {
                    var task = zwaveAPIService.SendCommand(byte.Parse(device.SourceId), command.Value, parameter);
                    sendCommandTasks.Add(task);
                }
            }

            if (device.Source == DeviceSource.Tuya)
            {
                var tuyaAPIService = serviceProvider.GetRequiredService<ITuyaAPIService>();
                var dpsData = tuyaAPIService.ConvertStateToDPS(action.DeviceEventToSend ?? DeviceEvent.Unknown, device.Kind, action.DeviceEventProperties ?? []);

                if (dpsData.Count != 0)
                {
                    var task = tuyaAPIService.SendCommand(device.SourceId, dpsData);
                    sendCommandTasks.Add(task);
                }
            }

            if (device.Source == DeviceSource.IkeaDirigera)
            {
                var ikeaDirigeraService = serviceProvider.GetRequiredService<IIkeaDirigeraService>();
                var payload = ikeaDirigeraService.ConvertStateToAction(action.DeviceEventToSend ?? DeviceEvent.Unknown);

                if (payload is not null)
                {
                    var task = ikeaDirigeraService.SendAction(device.SourceId, payload);
                    sendCommandTasks.Add(task);
                }
            }
        }

        await Task.WhenAll(sendCommandTasks);
    }

    private static string GetMessageToSend(ActionEntity action, object? source, List<DeviceEntity> devices)
    {
        if (string.IsNullOrWhiteSpace(action.MessageToSend))
            return string.Empty;

        string deviceString = string.Join(", ", devices.Select(d => d.Name));

        string message = action.MessageToSend.Replace("{devices}", deviceString);
        message = message.Replace("{source}", source?.ToString());
        message = message.Replace("{now}", DateTime.Now.ToString());

        return message;
    }
}
