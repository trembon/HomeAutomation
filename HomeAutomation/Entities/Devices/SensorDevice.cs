using HomeAutomation.Database;
using HomeAutomation.Database.Enums;
using HomeAutomation.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Entities.Devices
{
    public class SensorDevice : Device
    {
        public Dictionary<SensorValueType, SensorValue> LatestValues { get; }

        public SensorDevice()
        {
            this.LatestValues = new Dictionary<SensorValueType, SensorValue>();
        }
    }
}
