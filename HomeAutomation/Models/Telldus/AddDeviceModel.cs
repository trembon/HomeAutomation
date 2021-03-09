using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.Telldus
{
    public class AddDeviceModel
    {
        public string Name { get; set; }

        public string Protocol { get; set; }

        public string Model { get; set; }

        public string ParameterHouse { get; set; }

        public string ParameterUnit { get; set; }
    }
}
