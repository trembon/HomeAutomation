using Microsoft.Extensions.Logging;
using SlackNet;
using System.Collections.Concurrent;

namespace HomeAutomation.Core.Logging;

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
        GC.SuppressFinalize(this);
    }
}
