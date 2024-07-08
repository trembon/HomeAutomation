using HomeAutomation.Base.Enums;
using HomeAutomation.Core.Services;
using HomeAutomation.Database.Contexts;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Entities.Enums;
using HomeAutomation.Webhooks.Models.Telldus;
using HomeAutomation.Webhooks.Models.ZWave;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HomeAutomation.Webhooks
{
    [ApiController]
    [Route("webhooks/zwave")]
    public class ZWaveController : ControllerBase
    {
        private readonly ILogger<ZWaveController> logger;

        private readonly IJsonDatabaseService jsonDatabaseService;
        private readonly IZWaveAPIService zwaveAPIService;
        private readonly ITriggerService triggerService;

        public ZWaveController(IJsonDatabaseService jsonDatabaseService, IZWaveAPIService zwaveAPIService, ITriggerService triggerService, ILogger<ZWaveController> logger)
        {
            this.logger = logger;

            this.jsonDatabaseService = jsonDatabaseService;
            this.zwaveAPIService = zwaveAPIService;
            this.triggerService = triggerService;
        }

        [HttpPost("controllerstatus")]
        public ActionResult ControllerStatus(ControllerStatusModel model)
        {
            zwaveAPIService.SendEventMessage($"ControllerStatus: {model.Status}", model.Timestamp.ToLocalTime());
            return Ok();
        }

        [HttpPost("nodeupdate")]
        public async Task<ActionResult> NodeUpdate(NodeUpdateModel model)
        {
            zwaveAPIService.SendEventMessage($"NodeUpdate: {model.NodeId}, {model.ValueType}: {model.Value}", model.Timestamp.ToLocalTime());

            var device = jsonDatabaseService.Devices.FirstOrDefault(s => s.Source == DeviceSource.ZWave && s.SourceID == model?.NodeId.ToString());
            if (device != null)
            {
                var state = zwaveAPIService.ConvertParameterToEvent(device.GetType(), model.ValueType, model.Value);

                logger.LogInformation($"ZWave.NodeUpdate :: {device.ID} :: NodeId:{model.NodeId}, ValueType:{model.ValueType}: Value:{model.Value}, ValueObjectType:{model.Value.GetType().Name} MappedState:{state}");
                await triggerService.FireTriggersFromDevice(device, state);
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
}
