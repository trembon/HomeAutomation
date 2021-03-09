using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.ZWave
{
    public class ListNodesModel
    {
        public IEnumerable<NodeModel> Nodes { get; set; }
    }
}
