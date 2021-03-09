using HomeAutomation.Base.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.ZWaveWebhook
{
    public class ControllerStatusModel
    {
        public DateTime Timestamp { get; set; }

        public ZWaveControllerStatus Status { get; set; }
    }
}
