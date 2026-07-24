using System.Threading;
using System.Threading.Tasks;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Collector;

/// <summary>
/// An <see cref="IMeshFleetReadModel"/> composed from a pluggable <see cref="IMeshTraceSource"/> (X-Ray /
/// Tempo / Jaeger) for the trace-shaped read-models and one or more <see cref="IMeshUsageSource"/>s
/// (CloudWatch / App Insights) for per-topic stats — the backend-composed alternative to the in-memory
/// push-collector, so the fleet UI + waterfall work over the observability backends a team already runs.
/// See <c>work/otel-fleet-adapter-scope.md</c>.
/// </summary>
/// <remarks>
/// The two sources cover different, non-overlapping slices of the fleet, and each is fetched in
/// isolation (a throwing source degrades its own slice to empty, never blanks the whole view — the
/// aggregator's fetch-isolation rule):
/// <list type="bullet">
/// <item>Topics + their stats come from the usage feed. A usage backend has no descriptor and (for the
/// shipped CloudWatch/App-Insights adapters) no duration, so those dimensions are marked absent on each
/// topic row (<see cref="TopicSummary.MissingFeeds"/>) rather than shown as zero.</item>
/// <item>Recent flows and the (anonymous-but-live) service list come from the trace source. Traces are
/// sampled and carry no per-service counts, so service rows are name-only with their stat dimensions
/// marked absent (<see cref="ServiceSummary.MissingFeeds"/>) — the same "from traffic alone" rule the
/// push collector applies to an unregistered service.</item>
/// <item>A single service's detail and a single topic's catalog row are omitted (return null): neither a
/// trace store nor a usage feed can answer them here (no descriptor feed). They compose in a later increment.</item>
/// </list>
/// The error rule for topic stats follows the <c>result</c> tag vocabulary the usage feed carries
/// (<c>docs/mesh-usage-feed.md</c> §1), NOT the wire-status classifiers — see <see cref="IsErrorResult"/>.
/// </remarks>
public class CompositeMeshFleetReadModel : IMeshFleetReadModel
{
    /// <summary>Recent-flows cap, matching the push collector's <c>MaxFleetTraces</c>.</summary>
    private const int MaxFleetTraces = 20;

    private readonly IMeshTraceSource _traceSource;
    private readonly IEnumerable<IMeshUsageSource> _usageSources;

    public CompositeMeshFleetReadModel(IMeshTraceSource traceSource, IEnumerable<IMeshUsageSource> usageSources)
    {
        _traceSource = traceSource;
        _usageSources = usageSources;
    }

    public Task<TraceView?> TraceAsync(string traceId, CancellationToken cancellationToken = default)
        => _traceSource.GetTraceAsync(traceId, cancellationToken);

    public Task<CorrelationView?> CorrelationAsync(string correlationId, MeshTimeRange? range = null, CancellationToken cancellationToken = default)
        => _traceSource.GetCorrelationAsync(correlationId, range, cancellationToken);

    // No descriptor feed on this plane, so neither a single service nor a single topic can be answered
    // (the fleet view derives what it can from usage + traces; the per-entity pages compose later).
    public Task<ServiceView?> ServiceAsync(string name, MeshTimeRange? range = null, CancellationToken cancellationToken = default)
        => Task.FromResult<ServiceView?>(null);

    public Task<TopicSummary?> TopicAsync(string id, string? version, MeshTimeRange? range = null, CancellationToken cancellationToken = default)
        => Task.FromResult<TopicSummary?>(null);

    /// <summary>How close a usage source's returned window must be to the requested one to count as "honored"
    /// — absorbs a metrics backend snapping to its aggregation period, and the sub-second skew between the
    /// composite's <c>now</c> and each source's. A cumulative source returns a window starting far earlier than
    /// this tolerance, so it correctly reads as "did not honor".</summary>
    private static readonly TimeSpan WindowMatchTolerance = TimeSpan.FromMinutes(5);

    public async Task<FleetView> FleetAsync(MeshTimeRange? range = null, CancellationToken cancellationToken = default)
    {
        var requested = MeshTimeRangeResolver.Resolve(range, DateTimeOffset.UtcNow);
        var usageWindow = requested is { } w ? new MeshUsageWindow(w.From, w.To) : null;

        var (topics, reports) = await TopicsFromUsageAsync(usageWindow, cancellationToken);
        // Flows honor the picked window (X-Ray GetTraceSummaries / Tempo / Jaeger take the range) - and now the
        // counts do too, on any usage source that can honor it (CloudWatch/App-Insights). CompositeWindow decides
        // CountsWindowed from what the sources actually returned, so a source that can't window is still honest.
        var flows = await RecentFlowsAsync(range, cancellationToken);

        return new FleetView
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Topics = topics,
            Traces = flows,
            Services = ServicesFromFlows(flows),
            Window = CompositeWindow(requested, reports)
        };
    }

    /// <summary>Build the reported <see cref="MeshWindow"/> for the composite plane. Flows always honor the picked
    /// window. The counts honor it too when <em>every</em> contributing usage source returned a window matching the
    /// request (each windowable source queries its backend over exactly those bounds and echoes them back) — then
    /// <see cref="MeshWindow.CountsWindowed"/> is true. If any source couldn't (a cumulative source returns its own
    /// wider window), the counts are a union of differing windows, so <see cref="MeshWindow.CountsWindowed"/> is
    /// false and <see cref="MeshWindow.CountsSince"/> is the earliest window the counts actually cover. Honesty is
    /// decided from what the sources <em>returned</em>, never a source self-certifying. Null when no window was
    /// requested (today's shape).</summary>
    private static MeshWindow? CompositeWindow(
        (DateTimeOffset From, DateTimeOffset To)? requested, IReadOnlyList<MeshUsage> reports)
    {
        if (requested == null)
        {
            return null;
        }

        var (from, to) = requested.Value;
        var honored = reports.Count > 0 && reports.All(r =>
            r.WindowStartUtc is { } start && r.WindowEndUtc is { } end &&
            Within(start, from) && Within(end, to));

        DateTimeOffset? earliestStart = reports
            .Select(r => r.WindowStartUtc)
            .Where(start => start != null)
            .DefaultIfEmpty(null)
            .Min();

        return new MeshWindow
        {
            From = MeshTimeRangeResolver.ToIso(from),
            To = MeshTimeRangeResolver.ToIso(to),
            CountsWindowed = honored,
            CountsSince = honored || earliestStart == null ? null : MeshTimeRangeResolver.ToIso(earliestStart.Value)
        };
    }

    private static bool Within(DateTimeOffset actual, DateTimeOffset expected)
        => (actual - expected).Duration() <= WindowMatchTolerance;

    /// <summary>Merge every usage source's entries and roll them up into one <see cref="TopicSummary"/> per
    /// (topic, version), passing <paramref name="usageWindow"/> to each source. A failing source contributes
    /// nothing (its slice degrades to empty). Also returns each source's report, so the composite can decide from
    /// the returned windows whether the counts were actually windowed.</summary>
    private async Task<(List<TopicSummary> Topics, List<MeshUsage> Reports)> TopicsFromUsageAsync(
        MeshUsageWindow? usageWindow, CancellationToken cancellationToken)
    {
        var entries = new List<MeshUsageEntry>();
        var reports = new List<MeshUsage>();
        foreach (var source in _usageSources)
        {
            try
            {
                var usage = await source.FetchUsageAsync(usageWindow, cancellationToken);
                if (usage != null)
                {
                    reports.Add(usage);
                    if (usage.Entries is { Length: > 0 })
                    {
                        entries.AddRange(usage.Entries);
                    }
                }
            }
            catch
            {
                // Fetch isolation: one bad usage source leaves the others' topics intact rather than
                // failing the whole fleet view.
            }
        }

        var topics = entries
            .GroupBy(e => (e.Topic, e.Version ?? string.Empty))
            .OrderBy(g => g.Key.Item1, StringComparer.Ordinal).ThenBy(g => g.Key.Item2, StringComparer.Ordinal)
            .Select(TopicSummaryFromUsage)
            .ToList();
        return (topics, reports);
    }

    private static TopicSummary TopicSummaryFromUsage(IGrouping<(string Topic, string Version), MeshUsageEntry> group)
    {
        var statusCounts = new Dictionary<string, long>();
        long invocations = 0, errors = 0;
        double weightedDuration = 0;
        long durationSamples = 0;

        foreach (var entry in group)
        {
            invocations += entry.Count;
            if (IsErrorResult(entry.Status))
            {
                errors += entry.Count;
            }
            // Keep the raw result token verbatim (success/exception/failure/not-found/...), like the store
            // keeps raw statuses; a null result dimension can't be a key, so it's counted but not itemized.
            if (entry.Status is { } status)
            {
                statusCounts[status] = statusCounts.GetValueOrDefault(status) + entry.Count;
            }
            if (entry.AvgDurationMs is { } avg)
            {
                weightedDuration += avg * entry.Count;
                durationSamples += entry.Count;
            }
        }

        var summary = new TopicSummary
        {
            Topic = group.Key.Topic,
            Version = string.IsNullOrEmpty(group.Key.Version) ? null : group.Key.Version,
            Invocations = invocations,
            Errors = errors,
            AvgDurationMs = durationSamples > 0 ? weightedDuration / durationSamples : 0,
            StatusCounts = statusCounts
        };

        // Providers come from a descriptor feed this plane doesn't have; duration is absent unless a usage
        // source measured it. Mark both so the UI renders "—", not a fabricated zero.
        summary.MissingFeeds.Add("descriptor");
        if (durationSamples == 0)
        {
            summary.MissingFeeds.Add("duration");
        }

        return summary;
    }

    /// <summary>Whether a usage entry's <c>result</c> tag counts as an error. This is the metric's
    /// published <c>result</c> vocabulary (<c>docs/mesh-usage-feed.md</c> §1 / <c>MetricsExtensions.cs</c>),
    /// NOT the wire-status vocabulary: <c>success</c> collapses every ok/created/... outcome, <c>exception</c>
    /// and <c>failure</c> are error buckets with no specific status, and a verbatim failure status
    /// (<c>not-found</c>/<c>unauthorized</c>/...) is itemized. So the rule is "anything that isn't success,
    /// &lt;missing&gt;, or an absent dimension". Do NOT replace this with <c>BenzeneResultStatus.IsFailure</c>
    /// / <c>!IsSuccess</c> — they read the wire vocabulary and would miscount <c>exception</c>/<c>failure</c>
    /// as non-errors and <c>success</c> as an error.</summary>
    private static bool IsErrorResult(string? status)
        => status is not null && status != "success" && status != "<missing>";

    private async Task<List<TraceSummary>> RecentFlowsAsync(MeshTimeRange? range, CancellationToken cancellationToken)
    {
        try
        {
            var flows = await _traceSource.GetRecentFlowsAsync(MaxFleetTraces, range, cancellationToken);
            return flows.ToList();
        }
        catch
        {
            // Fetch isolation: a failing trace source degrades recent-flows (and the service list derived
            // from it) to empty rather than blanking the topics the usage feed supplied.
            return new List<TraceSummary>();
        }
    }

    /// <summary>The distinct services observed across recent flows, each an anonymous-but-live row: known
    /// only from traffic, so its per-service counts are genuinely absent (traces carry no per-service
    /// aggregate) and its health is unknown (no heartbeat feed).</summary>
    private static List<ServiceSummary> ServicesFromFlows(List<TraceSummary> flows)
    {
        return flows
            .SelectMany(f => f.Services)
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .OrderBy(s => s, StringComparer.Ordinal)
            .Select(name => new ServiceSummary
            {
                Service = name,
                Health = MeshHealth.Unknown,
                MissingFeeds = { "descriptor", "health", "stats" }
            })
            .ToList();
    }
}
