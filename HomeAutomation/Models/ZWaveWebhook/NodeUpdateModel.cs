using HomeAutomation.Base.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.ZWaveWebhook
{
    public class NodeUpdateModel
    {
        public byte NodeId { get; set; }
        
        public DateTime Timestamp { get; set; }

        public ZWaveEventParameter ValueType { get; set; }

        public object Value { get; set; }
    }
}
