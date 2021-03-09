using HomeAutomation.Base.Converters;
using HomeAutomation.Entities.Devices;
using HomeAutomation.Entities.Triggers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Entities
{
    public class MemoryEntities
    {
        [JsonProperty(ItemConverterType = typeof(BaseTypeConverter<Device>))]
        public List<Device> Devices { get; set; }

        [JsonProperty(ItemConverterType = typeof(BaseTypeConverter<Trigger>))]
        public List<Trigger> Triggers { get; set; }

        [JsonProperty(ItemConverterType = typeof(BaseTypeConverter<Action.Action>))]
        public List<Action.Action> Actions { get; set; }

        public MemoryEntities()
        {
            this.Devices = new List<Device>();
            this.Triggers = new List<Trigger>();
            this.Actions = new List<Action.Action>();
        }
    }
}
