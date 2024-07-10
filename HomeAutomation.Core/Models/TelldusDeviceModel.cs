using HomeAutomation.Base.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeAutomation.Core.Models
{
    public class TelldusDeviceModel
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public string Model { get; set; }

        public string Protocol { get; set; }

        public Dictionary<string, string> Parameters { get; set; }

        public TelldusDeviceMethods SupportedMethods { get; set; }

        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is TelldusDeviceModel model)
                return model.ID == this.ID;

            return false;
        }
    }
}
