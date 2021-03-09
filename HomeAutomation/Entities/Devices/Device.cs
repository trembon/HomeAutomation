using HomeAutomation.Base.Converters;
using HomeAutomation.Entities.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Entities.Devices
{
    public abstract class Device : IEntity
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public DeviceSource Source { get; set; }

        public string SourceID { get; set; }

        public override string ToString()
        {
            return $"{Name} (ID: {ID})";
        }

        public virtual string ToSourceString()
        {
            return $"länkning ({Name})";
        }
    }
}
