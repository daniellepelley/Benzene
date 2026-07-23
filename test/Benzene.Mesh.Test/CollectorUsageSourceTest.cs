using Benzene.Mesh.Collector;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Wire;
using Xunit;

namespace Benzene.Mesh.Test;

/// <summary>
/// The collector→aggregator usage bridge: cumulative per-topic status counts become
/// <c>usage.json</c> entries, at exactly the dimensions the trace feed really has (no transport,
/// no per-service attribution - the missing-dimension degradation path, exercised honestly).
/// </summary>
public class CollectorUsageSourceTest
{
    private static MeshTraceEvent Event(string spanId, string topic, string status, string? version = null)
    {
        return new MeshTraceEvent
        {
            TraceId = "trace-1",
            SpanId = spanId,
            Service = "orders-api",
            Topic = topic,
            TopicVersion = version,
            Status = status,
            DurationMs = 1,
            StartedAt = DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public async Task FetchUsageAsync_ReportsOneEntryPerTopicVersionStatus()
    {
        var store = new MeshCollectorStore();
        store.AddEvents(new[]
        {
            Event("s1", "orders:create", "created", "v1"),
            Event("s2", "orders:create", "created", "v1"),
            Event("s3", "orders:create", "validation-error", "v1"),
            Event("s4", "orders:get-all", "ok")
        });
        var at = new DateTimeOffset(2026, 7, 22, 9, 0, 0, TimeSpan.Zero);
        var source = new CollectorUsageSource(store, () => at);

        var usage = await source.FetchUsageAsync();

        Assert.NotNull(usage);
        Assert.Equal(at, usage!.GeneratedAtUtc);
        Assert.Equal(store.StartedAtUtc, usage.WindowStartUtc); // cumulative stats: window = since store start
        Assert.Equal(at, usage.WindowEndUtc);
        Assert.Equal(3, usage.Entries.Length);

        var created = Assert.Single(usage.Entries, e => e.Status == "created");
        Assert.Equal("orders:create", created.Topic);
        Assert.Equal("v1", created.Version);
        Assert.Equal(2, created.Count);
        Assert.Equal(MeshUsageSource.Collector, created.Source);
        // The trace wire shape carries neither dimension - reported as absent, never guessed.
        Assert.Null(created.Transport);
        Assert.Null(created.Service);

        Assert.Single(usage.Entries, e => e.Status == "validation-error" && e.Count == 1);
        Assert.Single(usage.Entries, e => e.Topic == "orders:get-all" && e.Status == "ok" && e.Version == null);
    }

    [Fact]
    public async Task FetchUsageAsync_EmptyStore_ReportsAWiredFeedWithNoEntries()
    {
        // Never null: a live collector with no traffic is "feed wired, nothing observed" (empty
        // entries), which the aggregator still publishes - distinct from no usage feed at all.
        var source = new CollectorUsageSource(new MeshCollectorStore());

        var usage = await source.FetchUsageAsync();

        Assert.NotNull(usage);
        Assert.Empty(usage!.Entries);
    }

    [Fact]
    public async Task FetchUsageAsync_NullStatusTraffic_ReportsANullStatusEntry()
    {
        // A null wire status is ingested as an empty-string count key; the usage entry surfaces
        // it as a genuinely absent status dimension rather than inventing a label.
        var store = new MeshCollectorStore();
        store.AddEvents(new[] { Event("s1", "orders:create", null!) });
        var source = new CollectorUsageSource(store);

        var usage = await source.FetchUsageAsync();

        var entry = Assert.Single(usage!.Entries);
        Assert.Null(entry.Status);
        Assert.Equal(1, entry.Count);
    }
}
