using HomeAutomation.Base.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.ZWaveWebhook
{
    public class DiscoveryProgressModel
    {
        public DateTime Timestamp { get; set; }

        public ZWaveDiscoveryStatus Status { get; set; }
    }
}
