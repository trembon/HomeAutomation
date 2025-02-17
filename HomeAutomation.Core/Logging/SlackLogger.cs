using Microsoft.Extensions.Logging;
using SlackNet;
using SlackNet.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace HomeAutomation.Base.Logging
{
    public class SlackLogger(string categoryName, ISlackApiClient slackApiClient) : ILogger
    {
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
                    message = $"{categoryName}: {formatter(state, exception)}\n{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}";

                var response = await slackApiClient.Chat.PostMessage(new Message
                {
                    Channel = "errors",
                    Text = message,
                });

                if (response is not null)
                    throw new Exception("Slack communication error.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - ERROR: Failed to send log to slack. ({ex.GetType().FullName}: {ex.Message})");
            }
        }
    }
}