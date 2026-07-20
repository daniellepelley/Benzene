using System.Collections.Generic;

namespace Benzene.Clients;

/// <summary>
/// Shared helper for <see cref="IBenzeneBatchMessageClient"/> implementations: splits a request
/// collection into provider-sized chunks while carrying each item's original index, so per-entry
/// failures can be reported back against the caller's collection.
/// </summary>
public static class BatchSend
{
    /// <summary>
    /// Splits <paramref name="items"/> into chunks of at most <paramref name="chunkSize"/>, pairing
    /// each item with its zero-based index in <paramref name="items"/>.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The items to chunk, in order.</param>
    /// <param name="chunkSize">The maximum chunk size (the provider's per-batch limit).</param>
    /// <returns>The chunks, each a list of (item, original index) pairs.</returns>
    public static IEnumerable<IReadOnlyList<(T Item, int Index)>> Chunk<T>(IReadOnlyCollection<T> items, int chunkSize)
    {
        var chunk = new List<(T, int)>(chunkSize);
        var index = 0;
        foreach (var item in items)
        {
            chunk.Add((item, index));
            index++;
            if (chunk.Count == chunkSize)
            {
                yield return chunk;
                chunk = new List<(T, int)>(chunkSize);
            }
        }

        if (chunk.Count > 0)
        {
            yield return chunk;
        }
    }
}
