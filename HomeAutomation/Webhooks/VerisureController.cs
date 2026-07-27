using HomeAutomation.Core.Services;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using HomeAutomation.Webhooks.Models.Verisure;
using Microsoft.AspNetCore.Mvc;

namespace HomeAutomation.Webhooks;

[ApiController]
[Route("webhooks/verisure")]
public class VerisureController(ISensorValueService sensorValueService, IDeviceRepository deviceRepository, IVerisureAPIService verisureAPIService, ITriggerService triggerService, IStateStoreService stateStoreService, ILogger<VerisureController> logger) : ControllerBase
{
    [HttpPost("deviceupdate")]
    public async Task<ActionResult> DeviceUpdate(DeviceUpdateModel model, CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetBySource(DeviceSource.Verisure, model.Id, cancellationToken);
        if (device != null)
        {
            var state = verisureAPIService.MapStateToDeviceEvent(model.State);

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Verisure.DeviceUpdate :: {deviceId} :: DeviceId:{sourceId}, MappedState:{state}", device.Id, model?.Id, state);
            await stateStoreService.SetDeviceState(device.Id, state, cancellationToken);
            await triggerService.FireTriggersFromDevice(device, state, cancellationToken);
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Verisure.DeviceUpdate :: Event from {sourceId}, but device is not mapped in database (State: {state})", model.Id, model.State);
        }
        return Ok();
    }

    [HttpPost("sensorupdate")]
    public async Task<ActionResult> SensorUpdate(SensorUpdateModel model, CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetBySource(DeviceSource.Verisure, model.Id, cancellationToken);

        if (device != null && device.Kind == DeviceKind.Sensor)
        {
            var sensorType = verisureAPIService.MapTypeToSensorKind(model.Type);

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Verisure.SensorUpdate :: {deviceId} :: NodeId:{nodeId}, ValueType:{valueType}: Value:{value}", device.Id, model.Id, sensorType, model?.Value);
            await sensorValueService.AddValue(device.Id, sensorType, model?.Value.ToString() ?? "", model?.Timestamp, cancellationToken);
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Verisure.SensorUpdate :: Sensor update from {sourceId}, but device is not mapped in database. (Value: {value}, Type: {type})", model?.Id, model?.Value, model?.Type);
        }

        return Ok();
    }
}
