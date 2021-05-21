using HomeAutomation.Base.Converters;
using HomeAutomation.Entities.Conditions;
using HomeAutomation.Models.Actions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Entities.Action
{
    public abstract class Action : IEntity
    {
        public int ID { get; set; }

        public string UniqueID => $"{nameof(Action)}_{ID}";

        public bool Disabled { get; set; }

        public int[] Devices { get; set; }

        [JsonProperty(ItemConverterType = typeof(BaseTypeConverter<Condition>))]
        public Condition[] Conditions { get; set; }

        public abstract Task Execute(ActionExecutionArguments arguments);

        public virtual string ToSourceString()
        {
            return null;
        }
    }
}
