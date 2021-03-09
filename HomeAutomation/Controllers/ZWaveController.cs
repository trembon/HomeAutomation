using HomeAutomation.Models.ZWave;
using HomeAutomation.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Controllers
{
    public class ZWaveController : Controller
    {
        private readonly IZWaveAPIService zwaveAPIService;

        public ZWaveController(IZWaveAPIService zwaveAPIService)
        {
            this.zwaveAPIService = zwaveAPIService;
        }

        public async Task<IActionResult> Index()
        {
            ListNodesModel model = new ListNodesModel();
            model.Nodes = await zwaveAPIService.GetNodes();
            return View(model);
        }
    }
}
