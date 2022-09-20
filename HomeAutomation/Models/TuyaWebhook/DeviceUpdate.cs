using System.Collections.Generic;

namespace HomeAutomation.Models.TuyaWebhook
{
    public class DeviceUpdate
    {
        public string DeviceId { get; set; }

        public Dictionary<int, object> Data { get; set; }
    }
}
