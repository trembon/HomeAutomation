using HomeAutomation.Core.Entities;
using HomeAutomation.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HomeAutomation.Entities.Action;

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
        if (Actions != null)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            string cancellationTokenId = $"{arguments.Source.UniqueID}|{UniqueID}";

            if (Extend)
            {
                if (delayCancellationTokens.TryRemove(cancellationTokenId, out CancellationTokenSource currentTokenSource))
                    currentTokenSource.Cancel();

                delayCancellationTokens.TryAdd(cancellationTokenId, cancellationTokenSource);
            }

            var scopeFactory = arguments.GetService<IServiceScopeFactory>();
            ThreadPool.QueueUserWorkItem(task => ExecuteDelayedActions(scopeFactory, this, cancellationTokenId, cancellationTokenSource.Token));
        }

        return Task.CompletedTask;
    }

    private static async void ExecuteDelayedActions(IServiceScopeFactory scopeFactory, DelayAction delayAction, string cancellationTokenId, CancellationToken cancellationToken)
    {
        using (var scope = scopeFactory.CreateScope())
        {
            scope.ServiceProvider.GetService<ILogger<DelayAction>>().LogInformation($"Delay.Sleeping :: {delayAction.ID} :: Delay:{delayAction.Delay}");
        }

        await Task.Delay(delayAction.Delay, cancellationToken).ContinueWith(tsk => { }, CancellationToken.None);

        delayCancellationTokens.TryRemove(cancellationTokenId, out _);
        if (cancellationToken.IsCancellationRequested)
            return;

        using (var scope = scopeFactory.CreateScope())
        {
            scope.ServiceProvider.GetService<ILogger<DelayAction>>().LogInformation($"Delay.WakingUp :: {delayAction.ID} :: Delay:{delayAction.Delay}, Actions:{string.Join(',', delayAction.Actions)}");

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
