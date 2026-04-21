using HomeAutomation.Core.Services;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using HomeAutomation.Webhooks.Models.FusionSolar;
using Microsoft.AspNetCore.Mvc;

namespace HomeAutomation.Webhooks;

[ApiController]
[Route("webhooks/fusionsolar")]
public class FusionSolarController(IDeviceRepository deviceRepository, IFusionSolarService fusionSolarService, ISensorValueService sensorValueService, ILogger<FusionSolarController> logger) : ControllerBase
{
    [HttpPost("sensorupdate")]
    public async Task<ActionResult> SensorUpdate(SensorUpdateModel model, CancellationToken cancellationToken)
    {
        var device = await deviceRepository.Get(DeviceSource.FusionSolar, model.Id, cancellationToken);

        if (device != null && device.Kind == DeviceKind.Sensor)
        {
            var sensorType = fusionSolarService.MapTypeToSensorKind(model.Property);

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("FusionSolar.SensorUpdate :: {deviceId} :: NodeId:{nodeId}, ValueType:{valueType}: Value:{value}", device.Id, model.Id, sensorType, model?.Value);

            await sensorValueService.AddValue(device.Id, sensorType, model?.Value.ToString() ?? "", model?.Timestamp, cancellationToken);
        }
        else
        {
            logger.LogError("FusionSolar.SensorUpdate :: Sensor update from {sourceId}, but device is not mapped in database. (Value: {value}, Type: {})", model?.Id, model?.Value, model?.Property);
        }

        return Ok();
    }
}
