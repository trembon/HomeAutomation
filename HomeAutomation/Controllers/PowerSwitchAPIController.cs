using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeAutomation.Base.Enums;
using HomeAutomation.Database;
using HomeAutomation.Entities;
using HomeAutomation.Entities.Action;
using HomeAutomation.Entities.Devices;
using HomeAutomation.Models.Actions;
using HomeAutomation.Models.PowerSwitch;
using HomeAutomation.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HomeAutomation.Controllers
{
    [Produces("application/json")]
    [Route("api/powerswitch")]
    public class PowerSwitchAPIController : Controller
    {
        private readonly ILogger<PowerSwitchAPIController> logger;

        private readonly IJsonDatabaseService jsonDatabaseService;
        private readonly IServiceProvider serviceProvider;

        public PowerSwitchAPIController(IJsonDatabaseService jsonDatabaseService, IServiceProvider serviceProvider, ILogger<PowerSwitchAPIController> logger)
        {
            this.logger = logger;

            this.jsonDatabaseService = jsonDatabaseService;
            this.serviceProvider = serviceProvider;
        }

        [Route("turnon")]
        public async Task<IActionResult> TurnOn(int id)
        {
            var powerSwitch = jsonDatabaseService.PowerSwitches.FirstOrDefault(ps => ps.ID == id);
            if (powerSwitch == null)
            {
                logger.LogError($"PowerSwitchAPI.TurnOn: No power switch found with ID '{id}'.");
                return Ok(new ChangeStatusModel { IsOn = false, Result = false });
            }

            ChangeStatusModel model = new ChangeStatusModel();
            try
            {
                var arguments = new ActionExecutionArguments(powerSwitch, new Device[] { powerSwitch }, serviceProvider);
                StateAction stateAction = new StateAction { Devices = new int[] { powerSwitch.ID }, State = Entities.Enums.DeviceState.On };
                await stateAction.Execute(arguments);

                logger.LogInformation($"PowerSwitchAPI.TurnOn: Successfully turned power switch ON with ID '{id}'.");

                model.IsOn = true;
                model.Result = true;
            }
            catch
            {
                logger.LogError($"PowerSwitchAPI.TurnOn: Failed to turn power switch ON with ID '{id}'.");
            }

            return Ok(model);
        }

        [Route("turnoff")]
        public async Task<IActionResult> TurnOff(int id)
        {
            var powerSwitch = jsonDatabaseService.PowerSwitches.FirstOrDefault(ps => ps.ID == id);
            if (powerSwitch == null)
            {
                logger.LogError($"PowerSwitchAPI.TurnOff: No power switch found with ID '{id}'.");
                return Ok(new ChangeStatusModel { IsOn = false, Result = false });
            }

            ChangeStatusModel model = new ChangeStatusModel();
            try
            {
                var arguments = new ActionExecutionArguments(powerSwitch, new Device[] { powerSwitch }, serviceProvider);
                StateAction stateAction = new StateAction { Devices = new int[] { powerSwitch.ID }, State = Entities.Enums.DeviceState.Off };
                await stateAction.Execute(arguments);

                logger.LogInformation($"PowerSwitchAPI.TurnOff: Successfully turned power switch OFF with ID '{id}'.");

                model.IsOn = false;
                model.Result = true;
            }
            catch
            {
                logger.LogError($"PowerSwitchAPI.TurnOff: Failed to turn power switch OFF with ID '{id}'.");
            }

            return Ok(model);
        }
    }
}