using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeAutomation.Models.Telldus;
using HomeAutomation.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeAutomation.Controllers
{
    public class TelldusController : Controller
    {
        private ITelldusAPIService telldusAPIService;

        public TelldusController(ITelldusAPIService telldusAPIService)
        {
            this.telldusAPIService = telldusAPIService;
        }

        public async Task<IActionResult> Index()
        {
            ListDevicesModel model = new ListDevicesModel();
            model.Devices = await telldusAPIService.GetDevices();
            return View(model);
        }

        [HttpGet]
        public IActionResult AddDevice()
        {
            AddDeviceModel model = new AddDeviceModel();
            model.Model = "selflearning-switch";
            model.Protocol = "arctech";
            model.ParameterHouse = new Random().Next(1, 67108863).ToString(); // TODO: should be dividable by 4
            model.ParameterUnit = "1";

            return View(model);
        }

        [HttpPost]
        public IActionResult AddDevice(AddDeviceModel addDeviceModel)
        {
            if (ModelState.IsValid)
            {
                var parameters = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(addDeviceModel.ParameterHouse))
                    parameters.Add("house", addDeviceModel.ParameterHouse);

                if (!string.IsNullOrWhiteSpace(addDeviceModel.ParameterUnit))
                    parameters.Add("unit", addDeviceModel.ParameterUnit);

                // TODO: implement
                //telldusCoreService.AddDevice(addDeviceModel.Name, addDeviceModel.Protocol, addDeviceModel.Model, parameters);
                return RedirectToAction("Index");
            }

            return View(addDeviceModel);
        }

        public IActionResult Remove(int deviceId)
        {

            // TODO: implement
            //telldusCoreService.RemoveDevice(deviceId);
            return RedirectToAction("Index");
        }
    }
}