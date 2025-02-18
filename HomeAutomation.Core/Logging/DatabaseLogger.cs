using HomeAutomation.Database;
using HomeAutomation.Database.Contexts;
using HomeAutomation.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeAutomation.Base.Logging;

public class DatabaseLogger(string categoryName, IServiceScope serviceScope, bool enabled) : ILogger
{
    public IDisposable BeginScope<TState>(TState state)
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return enabled;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        try
        {
            using DefaultContext db = serviceScope.ServiceProvider.GetRequiredService<DefaultContext>();
            using IDbContextTransaction transaction = db.Database.BeginTransaction();

            LogRow row = new LogRow
            {
                Level = logLevel,
                Category = categoryName,
                EventID = eventId.Id,
                Message = formatter(state, exception)
            };

            StringBuilder exceptionMessage = new();
            while (exception != null)
            {
                exceptionMessage.AppendLine(exception.Message);
                exceptionMessage.AppendLine(exception.StackTrace);

                exception = exception.InnerException;
            }
            row.Exception = exceptionMessage.ToString();

            db.Add(row);
            db.SaveChanges();

            transaction.Commit();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"{DateTime.Now} - ERROR: Failed to add log entry to database. ({ex.GetType().FullName}: {ex.Message})");
        }
    }
}
