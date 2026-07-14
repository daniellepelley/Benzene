using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Core.Middleware;
using Xunit;

namespace Benzene.Test.Core.Middleware.Streaming;

public class StreamOperatorsTest
{
    [Fact]
    public async Task Window_BatchesIntoFixedSizeWindows_WithASmallerFinalWindow()
    {
        var windows = new List<IReadOnlyList<int>>();

        await foreach (var window in ToAsyncEnumerable(new[] { 1, 2, 3, 4, 5 }).Window(2))
        {
            windows.Add(window);
        }

        Assert.Equal(3, windows.Count);
        Assert.Equal(new[] { 1, 2 }, windows[0]);
        Assert.Equal(new[] { 3, 4 }, windows[1]);
        Assert.Equal(new[] { 5 }, windows[2]);
    }

    [Fact]
    public async Task Window_WithExactMultiple_ProducesNoTrailingWindow()
    {
        var windows = new List<IReadOnlyList<int>>();

        await foreach (var window in ToAsyncEnumerable(new[] { 1, 2, 3, 4 }).Window(2))
        {
            windows.Add(window);
        }

        Assert.Equal(2, windows.Count);
        Assert.All(windows, w => Assert.Equal(2, w.Count));
    }

    [Fact]
    public async Task Window_SizeLessThanOne_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await foreach (var _ in ToAsyncEnumerable(new[] { 1 }).Window(0))
            {
            }
        });
    }

    [Fact]
    public async Task PartitionBy_GroupsByKey_PreservingOrderWithinAndAcrossKeys()
    {
        var events = new[]
        {
            (Partition: "a", Value: 1),
            (Partition: "b", Value: 2),
            (Partition: "a", Value: 3),
            (Partition: "b", Value: 4),
            (Partition: "a", Value: 5),
        };

        var partitions = new List<KeyValuePair<string, IReadOnlyList<(string Partition, int Value)>>>();

        await foreach (var partition in ToAsyncEnumerable(events).PartitionBy(e => e.Partition))
        {
            partitions.Add(partition);
        }

        // Keys in first-seen order: "a" then "b".
        Assert.Equal(new[] { "a", "b" }, partitions.Select(p => p.Key));
        // Order preserved within each key.
        Assert.Equal(new[] { 1, 3, 5 }, partitions[0].Value.Select(e => e.Value));
        Assert.Equal(new[] { 2, 4 }, partitions[1].Value.Select(e => e.Value));
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(T[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }
}
