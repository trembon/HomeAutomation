using HomeAutomation.Base.Converters;
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

        public int[] Devices { get; set; }

        public string UniqueID => $"{nameof(Action)}_{ID}";

        public abstract Task Execute(ActionExecutionArguments arguments);

        public virtual string ToSourceString()
        {
            return null;
        }
    }
}
