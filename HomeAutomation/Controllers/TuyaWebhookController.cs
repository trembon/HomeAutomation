using HomeAutomation.Entities.Enums;
using HomeAutomation.Hubs;
using HomeAutomation.Models.TuyaWebhook;
using HomeAutomation.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HomeAutomation.Controllers
{
    [Route("api/webhook")]
    [ApiController]
    public class TuyaWebhookController : Controller
    {
        private ILogger<TuyaWebhookController> logger;

        private readonly IJsonDatabaseService jsonDatabaseService;
        private readonly ITuyaAPIService tuyaAPIService;
        private readonly ITriggerService triggerService;
        private readonly IHubContext<LogHub> hubContext;
        private readonly IConfiguration configuration;

        public TuyaWebhookController(IJsonDatabaseService jsonDatabaseService, ITuyaAPIService tuyaAPIService, ITriggerService triggerService, IHubContext<LogHub> hubContext, IConfiguration configuration, ILogger<TuyaWebhookController> logger)
        {
            this.logger = logger;

            this.jsonDatabaseService = jsonDatabaseService;
            this.tuyaAPIService = tuyaAPIService;
            this.triggerService = triggerService;
            this.hubContext = hubContext;
            this.configuration = configuration;
        }

        [HttpPost("tuya/deviceupdate")]
        public async Task<ActionResult> DeviceUpdate(DeviceUpdate model)
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
}
