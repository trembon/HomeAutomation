﻿using HomeAutomation.Models.Actions;
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
        private static object delayLock = new();
        private static ConcurrentDictionary<string, CancellationTokenSource> delayCancellationTokens = new();

        public int[] Actions { get; set; }

        public TimeSpan Delay { get; set; }

        /// <summary>
        /// If this delay action is called again from the same source, cancel the old delay and create a new delay.
        /// </summary>
        public bool Extend { get; set; }

        public override Task Execute(IActionExecutionArguments arguments)
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
                    ThreadPool.QueueUserWorkItem(task => ExecuteDelayedActions(scopeFactory, this, cancellationTokenId, cancellationTokenSource.Token));
                }
            }

            return Task.CompletedTask;
        }

        private static async void ExecuteDelayedActions(IServiceScopeFactory scopeFactory, DelayAction delayAction, string cancellationTokenId, CancellationToken cancellationToken)
        {
            await Task.Delay(delayAction.Delay, cancellationToken);

            lock (delayLock)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                delayCancellationTokens.TryRemove(cancellationTokenId, out _);
            }

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
