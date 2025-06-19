using HomeAutomation.Core.Services;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using HomeAutomation.Webhooks.Models.ZWave;
using Microsoft.AspNetCore.Mvc;

namespace HomeAutomation.Webhooks;

[ApiController]
[Route("webhooks/zwave")]
public class ZWaveController(IDeviceRepository deviceRepository, IZWaveAPIService zwaveAPIService, ITriggerService triggerService, ILogger<ZWaveController> logger) : ControllerBase
{
    [HttpPost("controllerstatus")]
    public ActionResult ControllerStatus(ControllerStatusModel model)
    {
        zwaveAPIService.SendEventMessage($"ControllerStatus: {model.Status}", model.Timestamp.ToLocalTime());
        return Ok();
    }

    [HttpPost("nodeupdate")]
    public async Task<ActionResult> NodeUpdate(NodeUpdateModel model, CancellationToken cancellationToken)
    {
        zwaveAPIService.SendEventMessage($"NodeUpdate: {model.NodeId}, {model.ValueType}: {model.Value}", model.Timestamp.ToLocalTime());

        var device = await deviceRepository.GetDevice(DeviceSource.ZWave, model?.NodeId.ToString(), cancellationToken);
        if (device != null)
        {
            var state = zwaveAPIService.ConvertParameterToEvent(device.Kind, model?.ValueType, model?.Value);

            logger.LogInformation("ZWave.NodeUpdate :: {deviceId} :: NodeId:{nodeId}, ValueType:{valueType}: Value:{value}, ValueObjectType:{valueType} MappedState:{state}", device.Id, model?.NodeId, model?.ValueType, model?.Value, model?.Value?.GetType().Name, state);
            await triggerService.FireTriggersFromDevice(device, state, cancellationToken);
        }

        return Ok();
    }

    [HttpPost("discoveryprogress")]
    public ActionResult DiscoveryProgress(DiscoveryProgressModel model)
    {
        zwaveAPIService.SendEventMessage($"DiscoveryProgress: {model.Status}", model.Timestamp.ToLocalTime());
        return Ok();
    }

    [HttpPost("nodeoperationprogress")]
    public ActionResult NodeOperationProgress(NodeOperationProgressModel model)
    {
        zwaveAPIService.SendEventMessage($"NodeOperationProgress: {model.NodeId}, {model.Status}", model.Timestamp.ToLocalTime());
        return Ok();
    }

    [HttpPost("healprogress")]
    public ActionResult HealProgress(HealProgressModel model)
    {
        zwaveAPIService.SendEventMessage($"HealProgress: {model.Status}", model.Timestamp.ToLocalTime());
        return Ok();
    }
}
