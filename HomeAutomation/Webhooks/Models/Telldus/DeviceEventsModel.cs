using HomeAutomation.Base.Enums;

namespace HomeAutomation.Webhooks.Models.Telldus
{
    public class DeviceEventsModel
    {
        public int DeviceID { get; set; }

        public TelldusDeviceMethods Command { get; set; }

        public string Parameter { get; set; }
    }
}
