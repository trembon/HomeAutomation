using HomeAutomation.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.Conditions
{
    public class ConditionExecutionArguments
    {
        private IServiceProvider serviceProvider;

        public IEntity Source { get; set; }

        public ConditionExecutionArguments(IEntity source, IServiceProvider serviceProvider)
        {
            this.Source = source;
            this.serviceProvider = serviceProvider;
        }

        public T GetService<T>()
        {
            return serviceProvider.GetService<T>();
        }
    }
}
