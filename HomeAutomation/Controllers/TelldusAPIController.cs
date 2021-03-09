using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeAutomation.Base.Enums;
using HomeAutomation.Entities;
using HomeAutomation.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HomeAutomation.Controllers
{
    [Route("api/telldus")]
    [ApiController]
    public class TelldusAPIController : ControllerBase
    {
        private ITelldusAPIService telldusAPIService;

        public TelldusAPIController(ITelldusAPIService telldusAPIService)
        {
            this.telldusAPIService = telldusAPIService;
        }

        [Route("send")]
        public async Task<IActionResult> SendCommand(int deviceId, TelldusDeviceMethods command)
        {
            var result = await telldusAPIService.SendCommand(deviceId, command);
            return Ok(result);
        }
    }
}