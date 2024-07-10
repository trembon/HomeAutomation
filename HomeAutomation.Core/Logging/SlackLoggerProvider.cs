using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Base.Logging
{
    public class SlackLoggerProvider : ILoggerProvider
    {
        private string slackToken;
        private readonly ConcurrentDictionary<string, SlackLogger> loggers = new ConcurrentDictionary<string, SlackLogger>();

        public SlackLoggerProvider(IConfiguration configuration)
        {
            this.slackToken = configuration["Slack:Token"] ?? throw new InvalidOperationException("Configuration Slack:Token does not exist");
        }

        public ILogger CreateLogger(string categoryName)
        {
            return loggers.GetOrAdd(categoryName, name => new SlackLogger(categoryName, slackToken));
        }

        public void Dispose()
        {
            loggers.Clear();
        }
    }
}
