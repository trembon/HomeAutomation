using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeAutomation.Models.PowerSwitch;
using HomeAutomation.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeAutomation.Controllers
{
    public class PowerSwitchController : Controller
    {
        private IJsonDatabaseService jsonDatabaseService;

        public PowerSwitchController(IJsonDatabaseService jsonDatabaseService)
        {
            this.jsonDatabaseService = jsonDatabaseService;
        }

        public IActionResult Index()
        {
            var powerSwitches = jsonDatabaseService.PowerSwitches.OrderBy(ps => ps.Name).ToList();

            var model = new ListPowerSwitchModel();
            foreach (var ps in powerSwitches)
            {
                var lightModel = new PowerSwitchModel();
                lightModel.ID = ps.ID;
                lightModel.Name = ps.Name;

                // TODO: translate to the new model
                //lightModel.TurnOffAt = await scheduledJobService.GetNextScheduledTurnOff(ps.ID);
                //lightModel.TurnOnAt = await scheduledJobService.GetNextScheduledTurnOn(ps.ID);

                //TelldusDeviceMethods lastCommand = await telldusAPIService.GetLastCommand(lightModel.TellstickID);
                //lightModel.IsOn = lastCommand == TelldusDeviceMethods.TurnOn;
                lightModel.IsOn = false;

                model.PowerSwitches.Add(lightModel);
            }

            return View(model);
        }
    }
}