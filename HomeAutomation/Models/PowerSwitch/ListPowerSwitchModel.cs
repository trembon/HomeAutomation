using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.PowerSwitch
{
    public class ListPowerSwitchModel
    {
        public List<PowerSwitchModel> PowerSwitches { get; set; }

        public ListPowerSwitchModel()
        {
            PowerSwitches = new List<PowerSwitchModel>();
        }
    }
}
