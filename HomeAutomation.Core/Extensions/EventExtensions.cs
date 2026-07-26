using System;
using System.Collections.Generic;
using System.Text;

namespace HomeAutomation.Core.Extensions;

internal static class EventExtensions
{
    internal static void Trigger(this EventHandler eventHandler, object sender, EventArgs eventArgs)
    {
        eventHandler?.Invoke(sender, eventArgs);
    }

    internal static void Trigger<TEventArgs>(this EventHandler<TEventArgs> eventHandler, object sender, TEventArgs eventArgs) where TEventArgs : EventArgs
    {
        eventHandler?.Invoke(sender, eventArgs);
    }
}
