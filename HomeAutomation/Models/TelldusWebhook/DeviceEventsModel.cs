using HomeAutomation.Base.Enums;
using HomeAutomation.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.TelldusWebhook
{
    public class DeviceEventsModel
    {
        public int DeviceID { get; set; }

        public TelldusDeviceMethods Command { get; set; }

        public string Parameter { get; set; }
    }
}
