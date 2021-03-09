using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.TelldusWebhook
{
    public class TelldusRawDeviceEventsModel
    {
        public int ControllerID { get; set; }

        public string RawData { get; set; }
    }
}
