using HomeAutomation.Core.Entities;
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
        return Task.CompletedTask;
    }

    public override string ToSourceString()
    {
        return $"delay ({Delay})";
    }
}
