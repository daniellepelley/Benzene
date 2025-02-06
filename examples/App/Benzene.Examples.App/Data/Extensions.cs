using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;

namespace Benzene.Examples.App.Data;

public static class Extensions
{
    public static Task<IBenzeneResult<T>> AsTask<T>(this IBenzeneResult<T> source)
    {
        return Task.FromResult(source);
    }

    public static Task<IBenzeneResult> AsTask(this IBenzeneResult source)
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