using HomeAutomation.Entities;
using HomeAutomation.Entities.Devices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.Actions
{
    public class ActionExecutionArguments
    {
        private IServiceProvider serviceProvider;

        public IEnumerable<Device> Devices { get; }

        public IEntity Source { get; set; }

        public ActionExecutionArguments(IEntity source, IEnumerable<Device> devices, IServiceProvider serviceProvider)
        {
            this.Source = source;
            this.Devices = devices;
            this.serviceProvider = serviceProvider;
        }

        public T GetService<T>()
        {
            return serviceProvider.GetService<T>();
        }
    }
}
