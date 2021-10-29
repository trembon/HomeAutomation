using HomeAutomation.Entities;
using HomeAutomation.Entities.Devices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.Actions
{
    public interface IActionExecutionArguments
    {
        IEnumerable<Device> Devices { get; }

        IEntity Source { get; }

        T GetService<T>();
    }

    public class ActionExecutionArguments : IActionExecutionArguments
    {
        private readonly IServiceProvider serviceProvider;

        public IEnumerable<Device> Devices { get; }

        public IEntity Source { get; }

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
