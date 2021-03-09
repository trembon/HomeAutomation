using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeAutomation.Database;
using HomeAutomation.Entities;
using HomeAutomation.Models.Sensor;
using HomeAutomation.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeAutomation.Controllers
{
    public class SensorController : Controller
    {
        private readonly IJsonDatabaseService jsonDatabaseService;

        public SensorController(IJsonDatabaseService jsonDatabaseService)
        {
            this.jsonDatabaseService = jsonDatabaseService;
        }

        public IActionResult Index()
        {
            ListSensorsModel model = new ListSensorsModel();

            model.Sensors = jsonDatabaseService.Sensors.Select(s => new SensorListItemModel
            {
                TellstickID = int.Parse(s.SourceID),
                Name = s.Name
            }).ToList();

            foreach(var sensor in model.Sensors)
            {
                sensor.Values = jsonDatabaseService.Sensors.FirstOrDefault(x => x.Name == sensor.Name).LatestValues.ToDictionary(k => k.Key, v => v.Value.Value);
            }

            return View(model);
        }
    }
}