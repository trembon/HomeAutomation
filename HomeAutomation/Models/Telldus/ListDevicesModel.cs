using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.Telldus
{
    public class ListDevicesModel
    {
        public IEnumerable<DeviceModel> Devices { get; set; }
    }
}
