using HomeAutomation.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Base.Logging
{
    public class HubLogger : ILogger
    {
        private readonly string categoryName;
        private readonly IServiceProvider serviceProvider;
        private readonly IConfiguration configuration;

        //private IHubContext<LogHub> logHubContext;

        public HubLogger(string categoryName, IServiceProvider serviceProvider)
        {
            this.categoryName = categoryName;
            this.serviceProvider = serviceProvider;

            //this.configuration = serviceProvider.GetService<IConfiguration>();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // TODO: can we implement this in blazor?
            //if (configuration.GetValue<bool>("Logging:EnableWebsocketLogging")) // TODO: rewrite this to set a flag when first client connects to the hub
            //{
            //    if (logHubContext == null)
            //        logHubContext = serviceProvider.GetService<IHubContext<LogHub>>();

            //    if (logHubContext != null)
            //    {
            //        string message = $"{categoryName}: {formatter(state, exception)}";
            //        if (exception != null)
            //            message = $"{categoryName}: {formatter(state, exception)}\n{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}";

            //        logHubContext.Clients.All.SendAsync("ReceiveLogOutput", new { LogLevel = logLevel.ToString(), message });
            //    }
            //}
        }
    }
}
