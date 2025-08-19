using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HomeAutomation.Core.Logging;

public class DatabaseLoggerProvider(IServiceScopeFactory serviceScopeFactory) : ILoggerProvider
{
    private const string BASE_NAMESPACE = "HomeAutomation";

    private readonly ConcurrentDictionary<string, DatabaseLogger> loggers = new();

    public ILogger CreateLogger(string categoryName)
    {
        bool enabled = categoryName.StartsWith(BASE_NAMESPACE); // only enable database logging for this dll
        return loggers.GetOrAdd(categoryName, name => new DatabaseLogger(name, serviceScopeFactory.CreateScope(), enabled));
    }

    public void Dispose()
    {
        loggers.Clear();
        GC.SuppressFinalize(this);
    }
}
