using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeAutomation.Base.Enums;
using HomeAutomation.Base.Extensions;
using HomeAutomation.Database;
using HomeAutomation.Entities;
using HomeAutomation.Entities.Enums;
using HomeAutomation.Hubs;
using HomeAutomation.Models.ZWaveWebhook;
using HomeAutomation.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HomeAutomation.Controllers
{
    [Route("api/webhook")]
    [ApiController]
    public class ZWaveWebhookController : ControllerBase
    {
        private ILogger<ZWaveWebhookController> logger;

        private DefaultContext context;
        private readonly IJsonDatabaseService jsonDatabaseService;
        private readonly IZWaveAPIService zwaveAPIService;
        private readonly ITriggerService triggerService;
        private readonly IHubContext<LogHub> hubContext;
        private readonly IConfiguration configuration;

        public ZWaveWebhookController(DefaultContext context, IJsonDatabaseService jsonDatabaseService, IZWaveAPIService zwaveAPIService, ITriggerService triggerService, IHubContext<LogHub> hubContext, IConfiguration configuration, ILogger<ZWaveWebhookController> logger)
        {
            this.logger = logger;

            this.context = context;
            this.jsonDatabaseService = jsonDatabaseService;
            this.zwaveAPIService = zwaveAPIService;
            this.triggerService = triggerService;
            this.hubContext = hubContext;
            this.configuration = configuration;
        }

        [HttpPost("zwave/controllerstatus")]
        public async Task<ActionResult> ControllerStatus(ControllerStatusModel model)
        {
            await ZWaveLogStream(new { Timestamp = model.Timestamp.ToLocalTime(), Message = $"ControllerStatus: {model.Status}" });
            return Ok();
        }

        [HttpPost("zwave/nodeupdate")]
        public async Task<ActionResult> NodeUpdate(NodeUpdateModel model)
        {
            await ZWaveLogStream(new { Timestamp = model.Timestamp.ToLocalTime(), Message = $"NodeUpdate: {model.NodeId}, {model.ValueType}: {model.Value}" });

            var device = jsonDatabaseService.Devices.FirstOrDefault(s => s.Source == DeviceSource.ZWave && s.SourceID == model?.NodeId.ToString());
            if (device != null)
            {
                var state = zwaveAPIService.ConvertParameterToEvent(model.ValueType, model.Value);
                _ = triggerService.FireTriggersFromDevice(device, state);
            }

            return Ok();
        }

        [HttpPost("zwave/discoveryprogress")]
        public async Task<ActionResult> DiscoveryProgress(DiscoveryProgressModel model)
        {
            await ZWaveLogStream(new { Timestamp = model.Timestamp.ToLocalTime(), Message = $"DiscoveryProgress: {model.Status}" });
            return Ok();
        }

        [HttpPost("zwave/nodeoperationprogress")]
        public async Task<ActionResult> NodeOperationProgress(NodeOperationProgressModel model)
        {
            await ZWaveLogStream(new { Timestamp = model.Timestamp.ToLocalTime(), Message = $"NodeOperationProgress: {model.NodeId}, {model.Status}" });
            return Ok();
        }

        [HttpPost("zwave/healprogress")]
        public async Task<ActionResult> HealProgress(HealProgressModel model)
        {
            await ZWaveLogStream(new { Timestamp = model.Timestamp.ToLocalTime(), Message = $"HealProgress: {model.Status}" });
            return Ok();
        }

        private async Task ZWaveLogStream(object data)
        {
            // TODO: add different zwave hub
            // TODO: rewrite this to set a flag when first client connects to the hub
            if (configuration.GetValue<bool>("Logging:EnableWebsocketLogging"))
            {
                await hubContext.Clients.All.SendAsync("ReceiveZWaveLogStream", data);
            }
        }
    }
}