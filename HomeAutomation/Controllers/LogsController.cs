using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace HomeAutomation.Controllers
{
    public class LogsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult SystemStream()
        {
            return View();
        }

        public IActionResult TelldusStream()
        {
            return View();
        }

        public IActionResult ZWaveStream()
        {
            return View();
        }
    }
}
