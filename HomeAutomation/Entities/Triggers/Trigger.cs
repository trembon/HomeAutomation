using HomeAutomation.Base.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HomeAutomation.Entities.Triggers
{
    public abstract class Trigger : IEntity
    {
        public int ID { get; set; }

        public int[] Actions { get; set; }

        public virtual string ToSourceString()
        {
            return null;
        }
    }
}
