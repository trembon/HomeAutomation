using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Base.Logging
{
    public class HubLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, HubLogger> loggers = new ConcurrentDictionary<string, HubLogger>();
        private readonly IServiceProvider serviceProvider;

        public HubLoggerProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return loggers.GetOrAdd(categoryName, name => new HubLogger(categoryName, serviceProvider));
        }

        public void Dispose()
        {
            loggers.Clear();
        }
    }
}
