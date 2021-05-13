using HomeAutomation.Models.Actions;
using HomeAutomation.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HomeAutomation.Entities.Action
{
    public class DelayAction : Action
    {
        private static object delayLock = new object();
        private static ConcurrentDictionary<string, CancellationTokenSource> delayCancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>();

        public int[] Actions { get; set; }

        public TimeSpan Delay { get; set; }

        /// <summary>
        /// If this delay action is called again from the same source, cancel the old delay and create a new delay.
        /// </summary>
        public bool Extend { get; set; }

        public override Task Execute(ActionExecutionArguments arguments)
        {
            if(Actions != null)
            {
                lock (delayLock)
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    string cancellationTokenId = $"{arguments.Source.UniqueID}|{this.UniqueID}";

                    if (Extend)
                    {
                        if (delayCancellationTokens.TryRemove(cancellationTokenId, out CancellationTokenSource currentTokenSource))
                            currentTokenSource.Cancel();

                        delayCancellationTokens.TryAdd(cancellationTokenId, cancellationTokenSource);
                    }

                    var scopeFactory = arguments.GetService<IServiceScopeFactory>();
                    _ = Task.Delay(Delay, cancellationTokenSource.Token).ContinueWith(task => ExecuteDelayedActions(scopeFactory, this), cancellationTokenSource.Token);
                }
            }

            return Task.CompletedTask;
        }

        private static async void ExecuteDelayedActions(IServiceScopeFactory scopeFactory, DelayAction delayAction)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var executionService = scope.ServiceProvider.GetService<IActionExecutionService>();
                foreach (var action in delayAction.Actions)
                    await executionService.Execute(action, delayAction);
            }
        }

        public override string ToSourceString()
        {
            return $"delay ({Delay})";
        }
    }
}
