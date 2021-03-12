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
using HomeAutomation.Models.TelldusWebhook;
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
    public class TelldusWebhookController : ControllerBase
    {
        private static object duplicationRequestLock = new object();
        private static List<DuplicateRecord> duplicationRequestLog = new List<DuplicateRecord>();

        private ILogger<TelldusWebhookController> logger;

        private DefaultContext context;
        private readonly IJsonDatabaseService jsonDatabaseService;
        private readonly ITelldusAPIService telldusAPIService;
        private readonly ITriggerService triggerService;
        private readonly IHubContext<LogHub> hubContext;
        private readonly IConfiguration configuration;

        public TelldusWebhookController(DefaultContext context, IJsonDatabaseService jsonDatabaseService, ITelldusAPIService telldusAPIService, ITriggerService triggerService, IHubContext<LogHub> hubContext, IConfiguration configuration, ILogger<TelldusWebhookController> logger)
        {
            this.logger = logger;

            this.context = context;
            this.jsonDatabaseService = jsonDatabaseService;
            this.telldusAPIService = telldusAPIService;
            this.triggerService = triggerService;
            this.hubContext = hubContext;
            this.configuration = configuration;
        }

        [HttpPost("telldus/sensorupdates")]
        public async Task<ActionResult<bool>> TelldusSensorUpdate(SensorUpdatesModel model)
        {
            lock (duplicationRequestLock)
            {
                if (IsDuplicateRequest($"{model.SensorID}|{model.Type}|{model.Value}"))
                    return Ok(false);
            }

            await TelldusLogStream(new { Timestamp = model?.Timestamp, Message = $"DEVICE {model?.SensorID}: {model?.Type.ToString()} - {model?.Value}" });

            var sensor = jsonDatabaseService.Sensors.FirstOrDefault(s => s.Source == DeviceSource.Telldus && s.SourceID == model?.SensorID.ToString());
            if (sensor != null)
            {
                logger.LogInformation($"Received sensor update from sensor '{sensor}'.");
            }
            else
            {
                logger.LogInformation($"Received sensor update from telldus sensor with ID '{model?.SensorID}'.");
            }

            try
            {
                SensorValue sensorValue = new SensorValue();
                sensorValue.TellstickID = model.SensorID;
                sensorValue.Type = model.Type;
                sensorValue.Value = model.Value;
                sensorValue.Timestamp = model.Timestamp;

                await context.SensorValues.AddAsync(sensorValue);
                await context.SaveChangesAsync();

                if(sensor != null)
                    sensor.LatestValues[model.Type] = sensorValue;

                return Ok(true);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, $"Failed to insert sensor value for sensor '{model?.SensorID}' into database.");
                return StatusCode(500);
            }
        }

        [HttpPost("telldus/deviceevents")]
        public async Task<ActionResult<bool>> TelldusDeviceEvents(DeviceEventsModel model)
        {
            lock (duplicationRequestLock)
            {
                if (IsDuplicateRequest($"{model.DeviceID}|{model.Command}|{model.Parameter}"))
                    return Ok(false);
            }

            await TelldusLogStream(new { Message = $"DEVICEID {model?.DeviceID}: {model?.Command.ToString()} ({model?.Parameter})" });

            var device = jsonDatabaseService.Devices.FirstOrDefault(s => s.Source == DeviceSource.Telldus && s.SourceID == model?.DeviceID.ToString());
            if (device != null)
            {
                var state = telldusAPIService.ConvertCommandToEvent(model.Command);
                _ = triggerService.FireTriggersFromDevice(device, state);
            }

            return Ok(true);
        }

        [HttpPost("telldus/rawevents")]
        public async Task<ActionResult<bool>> TelldusRawEvents(TelldusRawDeviceEventsModel model)
        {
            lock (duplicationRequestLock)
            {
                if (IsDuplicateRequest(model.RawData))
                    return Ok(false);
            }

            await TelldusLogStream(new { Message = $"RAW: {model?.RawData} (Controller {model?.ControllerID})" });
            return Ok(true);
        }

        private async Task TelldusLogStream(object data)
        {
            // TODO: add different telldus hub
            // TODO: rewrite this to set a flag when first client connects to the hub
            if (configuration.GetValue<bool>("Logging:EnableWebsocketLogging"))
            {
                await hubContext.Clients.All.SendAsync("ReceiveTelldusLogStream", data);
            }
        }

        private bool IsDuplicateRequest(string request)
        {
            DateTime now = DateTime.UtcNow;

            int ignoreDuplicateWebhooksInSeconds = configuration.GetValue("Telldus:IgnoreDuplicateWebhooksInSeconds", 10);
            duplicationRequestLog.RemoveAll(dr => now.AddSeconds(-ignoreDuplicateWebhooksInSeconds) > dr.RequestTime);

            var record = duplicationRequestLog.FirstOrDefault(dr => dr.Request.Equals(request, StringComparison.InvariantCultureIgnoreCase));
            if (record == null)
            {
                duplicationRequestLog.Add(new DuplicateRecord( request, now));
                return false;
            }

            return true;
        }

        private record DuplicateRecord(string Request, DateTime RequestTime);
    }
}