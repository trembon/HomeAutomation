using Microsoft.Extensions.Logging;
using SlackAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace HomeAutomation.Base.Logging
{
    public class SlackLogger : ILogger
    {
        private readonly string categoryName;
        private readonly string slackToken;

        public SlackLogger(string categoryName, string slackToken)
        {
            this.categoryName = categoryName;
            this.slackToken = slackToken;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == LogLevel.Error;
        }

        public async void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            try
            {
                string message = $"{categoryName}: {formatter(state, exception)}";
                if (exception != null)
                    message =  $"{categoryName}: {formatter(state, exception)}\n{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}";

                SlackTaskClient slackClient = new SlackTaskClient(slackToken);
                var response = await slackClient.PostMessageAsync("errors", message);

                if (!response.ok)
                    throw new Exception("Slack communication error.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - ERROR: Failed to send log to slack. ({ex.GetType().FullName}: {ex.Message})");
            }
        }

        public class SendMessageModel
        {
            public bool Result { get; set; }

            public string ErrorMessage { get; set; }
        }
    }
}
