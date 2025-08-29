using HomeAutomation.Core.Services;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using HomeAutomation.Webhooks.Models.Tuya;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace HomeAutomation.Webhooks;

[ApiController]
[Route("webhooks/tuya")]
public class TuyaController(IDeviceRepository deviceRepository, ITuyaAPIService tuyaAPIService, ITriggerService triggerService, ILogger<TuyaController> logger) : ControllerBase
{
    [HttpPost("deviceupdate")]
    public async Task<ActionResult> DeviceUpdate(DeviceUpdateModel model, CancellationToken cancellationToken)
    {
        foreach (var data in model.Data)
        {
            if (data.Value is JsonElement je)
            {
                model.Data[data.Key] = je.GetRawText();
            }
        }

        var device = await deviceRepository.Get(DeviceSource.Tuya, model?.DeviceId, cancellationToken);
        if (device != null)
        {
            var state = tuyaAPIService.ConvertPropertyToEvent(device.Kind, model?.Data);

            logger.LogInformation("Tuya.DeviceUpdate :: {deviceId} :: DeviceId:{sourceId}, DPS:{json} MappedState:{state}", device.Id, model?.DeviceId, JsonSerializer.Serialize(model?.Data), state);
            await triggerService.FireTriggersFromDevice(device, state, cancellationToken);
        }
        else
        {
            logger.LogError("Tuya.DeviceUpdate :: Event from {sourceId}, but device is not mapped in database", model?.DeviceId);
        }

        return Ok();
    }
}
