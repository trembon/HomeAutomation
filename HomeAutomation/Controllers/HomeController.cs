using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HomeAutomation.Models;
using HomeAutomation.Models.Home;
using HomeAutomation.Services;

namespace HomeAutomation.Controllers
{
    public class HomeController : Controller
    {
        private readonly IJsonDatabaseService jsonDatabaseService;

        public HomeController(IJsonDatabaseService jsonDatabaseService)
        {
            this.jsonDatabaseService = jsonDatabaseService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult Configuration()
        {
            var model = new EditConfigurationRequestModel();
            model.Configuration = jsonDatabaseService.ReadConfiguration();
            return View(model);
        }

        [HttpPost]
        public IActionResult Configuration(EditConfigurationRequestModel model)
        {
            if (!jsonDatabaseService.SaveConfiguration(model.Configuration, out string error))
            {
                ModelState.AddModelError(nameof(model.Configuration), error);
                return View(model);
            }
            return RedirectToAction(nameof(Configuration));
        }
    }
}
