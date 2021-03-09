using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeAutomation.Database;
using HomeAutomation.Entities;
using HomeAutomation.Models.Camera;
using HomeAutomation.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeAutomation.Controllers
{
    public class CameraController : Controller
    {
        private readonly IJsonDatabaseService jsonDatabaseService;

        public CameraController(IJsonDatabaseService jsonDatabaseService)
        {
            this.jsonDatabaseService = jsonDatabaseService;
        }

        public IActionResult Index()
        {
            ListCameraModel model = new ListCameraModel();
            model.Cameras = jsonDatabaseService.Cameras.OrderBy(x => x.Name).ToList();

            return View(model);
        }
    }
}