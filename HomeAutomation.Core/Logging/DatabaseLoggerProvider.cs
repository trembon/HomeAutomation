using HomeAutomation.Database;
using HomeAutomation.Database.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace HomeAutomation.Base.Logging
{
    public class DatabaseLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, DatabaseLogger> loggers;
        private readonly string baseNamespace;
        private readonly IServiceProvider serviceProvider;

        public DatabaseLoggerProvider(IServiceProvider serviceProvider)
        {
            this.loggers = new ConcurrentDictionary<string, DatabaseLogger>();
            this.baseNamespace = "HomeAutomation";
            this.serviceProvider = serviceProvider;
        }

        public ILogger CreateLogger(string categoryName)
        {
            bool enabled = categoryName.StartsWith(baseNamespace); // only enable database logging for this dll
            return loggers.GetOrAdd(categoryName, name => new DatabaseLogger(name, serviceProvider.CreateScope(), enabled));
        }

        public void Dispose()
        {
            loggers.Clear();
        }
    }
}
