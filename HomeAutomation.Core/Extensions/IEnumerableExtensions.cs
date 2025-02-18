using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Base.Extensions;

internal static class IEnumerableExtensions
{
    internal static Queue<TItem> ToQueue<TItem>(this IEnumerable<TItem> collection)
    {
        return new Queue<TItem>(collection);
    }
}
