using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.PowerSwitch
{
    public class PowerSwitchModel
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public bool IsOn { get; set; }

        public DateTime? TurnOffAt { get; set; }

        public DateTime? TurnOnAt { get; set; }
    }
}
