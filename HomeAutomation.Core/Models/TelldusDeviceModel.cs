using HomeAutomation.Base.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeAutomation.Core.Models
{
    public class TelldusDeviceModel
    {
        [JsonProperty("id")]
        public int ID { get; internal set; }

        [JsonProperty("name")]
        public string Name { get; internal set; }

        [JsonProperty("model")]
        public string Model { get; internal set; }

        [JsonProperty("protocol")]
        public string Protocol { get; internal set; }

        [JsonProperty("parameters")]
        public Dictionary<string, string> Parameters { get; internal set; }

        [JsonProperty("supportedmethods")]
        public TelldusDeviceMethods SupportedMethods { get; internal set; }

        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is TelldusDeviceModel)
                return ((TelldusDeviceModel)obj).ID == this.ID;

            return false;
        }
    }
}
