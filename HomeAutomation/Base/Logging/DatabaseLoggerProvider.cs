using HomeAutomation.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Base.Logging
{
    public class DatabaseLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, DatabaseLogger> loggers;
        private readonly string baseNamespace;

        private DbContextOptions<LogContext> dbContextOptions;

        public DatabaseLoggerProvider()
        {
            this.loggers = new ConcurrentDictionary<string, DatabaseLogger>();
            this.baseNamespace = typeof(Startup).Namespace;
        }

        public DatabaseLoggerProvider Configure(Action<DbContextOptionsBuilder<LogContext>> options)
        {
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<LogContext>();
            options(dbContextOptionsBuilder);

            dbContextOptions = dbContextOptionsBuilder.Options;
            return this;
        }

        public ILogger CreateLogger(string categoryName)
        {
            bool enabled = categoryName.StartsWith(baseNamespace); // only enable database logging for this dll
            return loggers.GetOrAdd(categoryName, name => new DatabaseLogger(name, dbContextOptions, enabled));
        }

        public void Dispose()
        {
            loggers.Clear();
        }
    }
}
