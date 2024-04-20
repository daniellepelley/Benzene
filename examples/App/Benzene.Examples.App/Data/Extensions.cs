using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Core.Results;

namespace Benzene.Examples.App.Data;

public static class Extensions
{
    public static Task<IClientResult<T>> AsTask<T>(this IClientResult<T> source)
    {
        return Task.FromResult(source);
    }

    public static Task<IClientResult> AsTask(this IClientResult source)
    {
        return Task.FromResult(source);
    }

    public static void Remove<T>(this ConcurrentBag<T> source, T item)
        where T : class
    {
        var items = source.ToArray().Where(x => x != item).ToArray();

        source.Clear();

        foreach (var itemToAdd in items)
        {
            source.Add(itemToAdd);
        }
    }
}