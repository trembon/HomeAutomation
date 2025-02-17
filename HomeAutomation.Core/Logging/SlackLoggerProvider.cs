using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SlackNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Base.Logging
{
    public class SlackLoggerProvider(ISlackApiClient slackApiClient) : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, SlackLogger> loggers = new();

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
        {
            return loggers.GetOrAdd(categoryName, name => new SlackLogger(categoryName, slackApiClient));
        }

        public void Dispose()
        {
            loggers.Clear();
        }
    }
}