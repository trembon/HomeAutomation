using HomeAutomation.Models.Tuya;
using HomeAutomation.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HomeAutomation.Controllers
{
    public class TuyaController : Controller
    {
        private readonly ITuyaAPIService tuyaAPIService;

        public TuyaController(ITuyaAPIService tuyaAPIService)
        {
            this.tuyaAPIService = tuyaAPIService;
        }

        public async Task<IActionResult> Index()
        {
            ListDevicesModel model = new ListDevicesModel();
            model.Devices = await tuyaAPIService.GetDevices();
            return View(model);
        }
    }
}
