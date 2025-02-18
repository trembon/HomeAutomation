using HomeAutomation.Core.Services;
using HomeAutomation.Database.Enums;
using HomeAutomation.Webhooks.Models.Tuya;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace HomeAutomation.Webhooks;

[ApiController]
[Route("webhooks/tuya")]
public class TuyaController : ControllerBase
{
    private readonly ILogger<TuyaController> logger;

    private readonly IJsonDatabaseService jsonDatabaseService;
    private readonly ITuyaAPIService tuyaAPIService;
    private readonly ITriggerService triggerService;

    public TuyaController(IJsonDatabaseService jsonDatabaseService, ITuyaAPIService tuyaAPIService, ITriggerService triggerService, ILogger<TuyaController> logger)
    {
        this.logger = logger;

        this.jsonDatabaseService = jsonDatabaseService;
        this.tuyaAPIService = tuyaAPIService;
        this.triggerService = triggerService;
    }

    [HttpPost("deviceupdate")]
    public async Task<ActionResult> DeviceUpdate(DeviceUpdateModel model)
    {
        foreach (var data in model.Data)
        {
            if (data.Value is JsonElement je)
            {
                model.Data[data.Key] = je.GetRawText();
            }
        }

        var device = jsonDatabaseService.Devices.FirstOrDefault(s => s.Source == DeviceSource.Tuya && s.SourceID == model?.DeviceId);
        if (device != null)
        {
            var state = tuyaAPIService.ConvertPropertyToEvent(device.GetType(), model.Data);

            logger.LogInformation($"Tuya.DeviceUpdate :: {device.ID} :: DeviceId:{model.DeviceId}, DPS:{JsonSerializer.Serialize(model.Data)} MappedState:{state}");
            await triggerService.FireTriggersFromDevice(device, state);
        }

        return Ok();
    }
}
