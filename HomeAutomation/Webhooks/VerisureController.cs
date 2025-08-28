using HomeAutomation.Core.Services;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using HomeAutomation.Webhooks.Models.Verisure;
using Microsoft.AspNetCore.Mvc;

namespace HomeAutomation.Webhooks;

[ApiController]
[Route("webhooks/verisure")]
public class VerisureController(IRepository<SensorValueEntity> repository, IDeviceRepository deviceRepository, IVerisureAPIService verisureAPIService, ITriggerService triggerService, ILogger<VerisureController> logger) : ControllerBase
{
    [HttpPost("deviceupdate")]
    public async Task<ActionResult> DeviceUpdate(DeviceUpdateModel model, CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetDevice(DeviceSource.Verisure, model.Id, cancellationToken);
        if (device != null)
        {
            var state = verisureAPIService.MapStateToDeviceEvent(model.State);

            logger.LogInformation("Verisure.DeviceUpdate :: {deviceId} :: DeviceId:{sourceId}, MappedState:{state}", device.Id, model?.Id, state);
            await triggerService.FireTriggersFromDevice(device, state, cancellationToken);
        }
        else
        {
            logger.LogError("Verisure.DeviceUpdate :: Event from {sourceId}, but device is not mapped in database (State: {state})", model.Id, model.State);
        }
        return Ok();
    }

    [HttpPost("sensorupdate")]
    public async Task<ActionResult> SensorUpdate(SensorUpdateModel model, CancellationToken cancellationToken)
    {
        //zwaveAPIService.SendEventMessage($"NodeUpdate: {model.NodeId}, {model.ValueType}: {model.Value}", model.Timestamp.ToLocalTime());

        var device = await deviceRepository.GetDevice(DeviceSource.Verisure, model.Id, cancellationToken);

        if (device != null && device.Kind == DeviceKind.Sensor)
        {
            var sensorType = verisureAPIService.MapTypeToSensorKind(model.Type);

            logger.LogInformation("Verisure.SensorUpdate :: {deviceId} :: NodeId:{nodeId}, ValueType:{valueType}: Value:{value}", device.Id, model.Id, sensorType, model?.Value);
            await repository.AddAndSave(new()
            {
                DeviceId = device.Id,
                Type = sensorType,
                Value = model?.Value.ToString() ?? "",
                Timestamp = model?.Timestamp ?? DateTime.UtcNow,
            }, cancellationToken);
        }
        else
        {
            logger.LogError("Verisure.SensorUpdate :: Sensor update from {sourceId}, but device is not mapped in database. (Value: {value}, Type: {})", model?.Id, model?.Value, model?.Type);
        }

        return Ok();
    }
}
