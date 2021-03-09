using HomeAutomation.Database.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.Sensor
{
    public class SensorListItemModel
    {
        public int ID { get; set; }

        public int TellstickID { get; set; }

        public string Name { get; set; }

        public Dictionary<SensorValueType, string> Values { get; set; }
    }
}
