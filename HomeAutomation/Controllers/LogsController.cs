using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeAutomation.Database;
using Microsoft.AspNetCore.Mvc;

namespace HomeAutomation.Controllers
{
    public class LogsController : Controller
    {
        private readonly LogContext logContext;

        public LogsController(LogContext logContext)
        {
            this.logContext = logContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Stored()
        {
            var logs = logContext.Rows.OrderByDescending(x => x.Timestamp).Take(300).ToList();
            return View(logs);
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
