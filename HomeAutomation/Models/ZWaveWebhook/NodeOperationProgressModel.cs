using HomeAutomation.Base.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.ZWaveWebhook
{
    public class NodeOperationProgressModel
    {
        public byte NodeId { get; set; }

        public DateTime Timestamp { get; set; }

        public ZWaveNodeQueryStatus Status { get; set; }
    }
}
