using HomeAutomation.Database.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.TelldusWebhook
{
    public class SensorUpdatesModel
    {
        public int SensorID { get; set; }
        
        public SensorValueType Type { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        public string Value { get; set; }
    }
}
