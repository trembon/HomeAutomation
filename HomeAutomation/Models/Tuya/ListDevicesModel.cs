using System.Collections.Generic;

namespace HomeAutomation.Models.Tuya
{
    public class ListDevicesModel
    {
        public IEnumerable<DeviceModel> Devices { get; set; }
    }
}
