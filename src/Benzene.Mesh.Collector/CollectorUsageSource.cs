using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Collector;

/// <summary>
/// Bridges a co-hosted collector's cumulative per-topic stats into the aggregator's usage feed
/// (<c>usage.json</c>) - the first shipped <see cref="IMeshUsageSource"/>, and the
/// "<c>IMeshArtifactStore</c> bridge to the aggregator pipeline" extension point this package's
/// docs always named. Register it alongside <c>AddMeshAggregator</c> in a host that also runs the
/// collector's handlers (they share the singleton <see cref="MeshCollectorStore"/>).
/// </summary>
/// <remarks>
/// Reports one entry per (topic, version, status) from the store's cumulative
/// <c>StatusCounts</c>. The trace wire shape (<c>docs/specification/mesh.md</c>) carries no
/// transport, and the store's per-status counts aren't attributed per handling service, so
/// <see cref="MeshUsageEntry.Transport"/> and <see cref="MeshUsageEntry.Service"/> are
/// deliberately <c>null</c> rather than guessed - a collector-fed usage feed exercises the
/// consumer-side missing-dimension degradation path honestly. A metrics-backend adapter (reading
/// the <c>benzene.messages.processed</c> counter's <c>transport</c> tag - see
/// <c>docs/mesh-usage-feed.md</c>) is what fills those dimensions in.
/// </remarks>
public class CollectorUsageSource : IMeshUsageSource
{
    private readonly MeshCollectorStore _store;
    private readonly Func<DateTimeOffset> _clock;

    /// <summary>Initializes a new instance of the <see cref="CollectorUsageSource"/> class.</summary>
    /// <param name="store">The collector store to report from.</param>
    /// <param name="clock">Supplies the current time; defaults to <see cref="DateTimeOffset.UtcNow"/>. Overridable for deterministic tests.</param>
    public CollectorUsageSource(MeshCollectorStore store, Func<DateTimeOffset>? clock = null)
    {
        _store = store;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Never returns <c>null</c>: a live collector with no traffic yet is a wired feed with
    /// nothing observed (an empty entries array), not an absent one. The window always starts at
    /// the store's own start - the in-memory stats are cumulative since process start, so a
    /// requested <paramref name="window"/> is deliberately <b>ignored</b> (these counters can't be
    /// sub-windowed) and the report keeps its since-start window. A caller comparing the reported
    /// window to what it asked for therefore sees they differ and keeps <c>CountsWindowed=false</c>.
    /// </remarks>
    public Task<MeshUsage?> FetchUsageAsync(MeshUsageWindow? window = null, CancellationToken cancellationToken = default)
    {
        var entries = _store.Fleet().Topics
            .SelectMany(topic => topic.StatusCounts
                .OrderBy(statusCount => statusCount.Key, StringComparer.Ordinal)
                .Select(statusCount => new MeshUsageEntry(
                    topic.Topic,
                    topic.Version,
                    service: null,
                    transport: null,
                    status: statusCount.Key.Length == 0 ? null : statusCount.Key,
                    statusCount.Value,
                    avgDurationMs: null,
                    MeshUsageSource.Collector)))
            .ToArray();

        return Task.FromResult<MeshUsage?>(new MeshUsage(_clock(), _store.StartedAtUtc, _clock(), entries));
    }
}
