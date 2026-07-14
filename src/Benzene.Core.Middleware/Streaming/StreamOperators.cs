using System.Runtime.CompilerServices;

namespace Benzene.Core.Middleware;

/// <summary>
/// Composable operators over an <see cref="IAsyncEnumerable{T}"/> stream, for use inside a
/// <c>UseStream(...)</c> step. Implemented as async-enumerable transforms (rather than middleware) so
/// they compose with LINQ-style pipelines and stay independent of any transport — the natural shape
/// for stream processing.
/// </summary>
public static class StreamOperators
{
    /// <summary>
    /// Batches the stream into fixed-size windows. The final window may be smaller than
    /// <paramref name="size"/>. Order is preserved. This is the building block for batch aggregation —
    /// e.g. writing one database round-trip per window instead of one per item.
    /// </summary>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <param name="source">The source stream.</param>
    /// <param name="size">The maximum number of items per window (must be at least 1).</param>
    /// <param name="cancellationToken">Cancellation for the enumeration.</param>
    /// <returns>A stream of windows, each a read-only list of up to <paramref name="size"/> items.</returns>
    public static async IAsyncEnumerable<IReadOnlyList<TItem>> Window<TItem>(
        this IAsyncEnumerable<TItem> source,
        int size,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (size < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Window size must be at least 1.");
        }

        var window = new List<TItem>(size);

        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            window.Add(item);

            if (window.Count == size)
            {
                yield return window;
                window = new List<TItem>(size);
            }
        }

        if (window.Count > 0)
        {
            yield return window;
        }
    }

    /// <summary>
    /// Groups the stream into per-key sub-streams, preserving item order within each key and yielding
    /// keys in the order they were first seen. Use this to restore per-partition ordering that the
    /// concurrent fan-out model loses — e.g. <c>PartitionBy(e =&gt; e.PartitionId)</c>.
    /// </summary>
    /// <remarks>
    /// This buffers the whole stream to group it, so it's suited to bounded batches (the typical
    /// Event Hubs / SQS / Kafka trigger batch), not unbounded infinite streams.
    /// </remarks>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <typeparam name="TKey">The partition key type.</typeparam>
    /// <param name="source">The source stream.</param>
    /// <param name="keySelector">Selects the partition key for an item.</param>
    /// <param name="cancellationToken">Cancellation for the enumeration.</param>
    /// <returns>A stream of (key, ordered items) pairs, keys in first-seen order.</returns>
    public static async IAsyncEnumerable<KeyValuePair<TKey, IReadOnlyList<TItem>>> PartitionBy<TItem, TKey>(
        this IAsyncEnumerable<TItem> source,
        Func<TItem, TKey> keySelector,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var groups = new Dictionary<TKey, List<TItem>>();
        var order = new List<TKey>();

        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            var key = keySelector(item);

            if (!groups.TryGetValue(key, out var items))
            {
                items = new List<TItem>();
                groups[key] = items;
                order.Add(key);
            }

            items.Add(item);
        }

        foreach (var key in order)
        {
            yield return new KeyValuePair<TKey, IReadOnlyList<TItem>>(key, groups[key]);
        }
    }
}
