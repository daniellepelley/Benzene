using System.Globalization;
using Amazon.XRay;
using Amazon.XRay.Model;
using Benzene.Mesh.Collector;
using Benzene.Mesh.Wire;
// Both namespaces declare a TraceSummary; the alias resolves the bare name to the mesh one (what this
// adapter produces) while X-Ray's own summary type stays reachable by its full name.
using TraceSummary = Benzene.Mesh.Collector.TraceSummary;

namespace Benzene.Mesh.Fleet.Aws.XRay;

/// <summary>
/// An <see cref="IMeshTraceSource"/> that answers <c>mesh:query:trace</c> and
/// <c>mesh:query:correlation</c> from AWS X-Ray: it fetches a trace's segments with
/// <c>BatchGetTraces</c> and maps the topic-bearing spans into a <see cref="TraceView"/> (see
/// <see cref="XRaySegmentMapper"/>), and finds a business correlation id's flows with
/// <c>GetTraceSummaries</c> filtered on the correlation-id annotation. This is the AWS realisation of the
/// trace-backed fleet reader scoped in <c>work/otel-fleet-adapter-scope.md</c> - the fleet UI's trace
/// waterfall and correlation triage over an existing observability backend, no push collector required.
/// </summary>
/// <remarks>
/// Trace stats and service health are deliberately not sourced here (see <see cref="IMeshTraceSource"/>);
/// X-Ray traces are sampled, so counts would be biased, and X-Ray has no heartbeat feed. Those compose
/// from an <c>IMeshUsageSource</c> (CloudWatch) and the heartbeat plane in later increments.
/// </remarks>
public class XRayTraceSource : IMeshTraceSource
{
    // X-Ray's BatchGetTraces accepts at most 5 trace ids per call.
    private const int BatchGetTracesMax = 5;

    private readonly IAmazonXRay _xray;
    private readonly XRayTraceSourceOptions _options;

    /// <summary>Creates the source over an X-Ray client (region/credentials come from the client).</summary>
    public XRayTraceSource(IAmazonXRay xray) : this(xray, new XRayTraceSourceOptions())
    {
    }

    /// <summary>Creates the source over an X-Ray client with explicit tuning (correlation lookback).</summary>
    public XRayTraceSource(IAmazonXRay xray, XRayTraceSourceOptions options)
    {
        _xray = xray;
        _options = options;
    }

    /// <summary>Fetches the trace's segments from X-Ray and maps its topic-bearing spans into a
    /// <see cref="TraceView"/>, or null when X-Ray has no such trace or it carried no Benzene spans.</summary>
    public async Task<TraceView?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(traceId))
        {
            return null;
        }

        var events = await FetchEventsAsync(new[] { traceId }, traceId, cancellationToken);

        // A null (rather than empty) view when X-Ray has no such trace, or a real trace that carried no
        // Benzene topic-bearing span - so the query handler answers NotFound, not an empty waterfall.
        return events.Count == 0 ? null : new TraceView { TraceId = traceId, Events = events };
    }

    /// <summary>Finds every X-Ray trace carrying the correlation-id annotation over the configured
    /// lookback window, maps each to a <see cref="TraceView"/>, and groups them into a
    /// <see cref="CorrelationView"/> (traces ordered by earliest start), or null when none matched.</summary>
    public async Task<CorrelationView?> GetCorrelationAsync(string correlationId, MeshTimeRange? range = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(correlationId))
        {
            return null;
        }

        var (start, end) = ResolveWindow(range, _options.CorrelationLookback);
        // benzene.correlation-id lands in X-Ray as the underscore-sanitised annotation key; only
        // annotations are filterable (see work/otel-fleet-adapter-scope.md §6b).
        var filter = $"annotation.benzene_correlation_id = \"{Escape(correlationId)}\"";

        var traceIds = new List<string>();
        string? nextToken = null;
        do
        {
            var summaries = await _xray.GetTraceSummariesAsync(new GetTraceSummariesRequest
            {
                StartTime = start,
                EndTime = end,
                FilterExpression = filter,
                NextToken = nextToken
            }, cancellationToken);

            if (summaries.TraceSummaries != null)
            {
                traceIds.AddRange(summaries.TraceSummaries.Select(s => s.Id).Where(id => !string.IsNullOrEmpty(id)));
            }

            nextToken = string.IsNullOrEmpty(summaries.NextToken) ? null : summaries.NextToken;
        }
        while (nextToken != null);

        if (traceIds.Count == 0)
        {
            return null;
        }

        var traces = new List<TraceView>();
        foreach (var batch in Chunk(traceIds, BatchGetTracesMax))
        {
            var response = await _xray.BatchGetTracesAsync(
                new BatchGetTracesRequest { TraceIds = batch }, cancellationToken);

            foreach (var trace in response.Traces ?? new List<Trace>())
            {
                if (trace.Segments is not { Count: > 0 } segments || string.IsNullOrEmpty(trace.Id))
                {
                    continue;
                }

                var events = XRaySegmentMapper.Map(trace.Id, segments.Select(s => s.Document));
                if (events.Count > 0)
                {
                    traces.Add(new TraceView { TraceId = trace.Id, Events = events });
                }
            }
        }

        if (traces.Count == 0)
        {
            return null;
        }

        // Earliest-first, the same ordering the in-memory collector's correlation view uses so the UI
        // renders both identically.
        traces.Sort((a, b) => EarliestStart(a).CompareTo(EarliestStart(b)));
        return new CorrelationView { CorrelationId = correlationId, Traces = traces };
    }

    /// <summary>Lists the most recent flows for the fleet view: one <c>GetTraceSummaries</c> over the
    /// recent-flows window (no filter), the top-N (by trace-id epoch) enriched with real span data via a
    /// bounded <c>BatchGetTraces</c> (≤4 calls for 20 rows — see
    /// <see cref="XRayTraceSourceOptions.RecentFlowsServiceEnrichmentMax"/>), mapped to
    /// <see cref="TraceSummary"/> rows and ordered newest-first. Enrichment reads each row's service names
    /// from the pipeline-stamped <c>benzene.service</c> (not X-Ray's <c>ServiceIds</c>, which on Lambda are
    /// infra/handler names), its real millisecond start (not the second-granularity id epoch), and its real
    /// span count. A row whose trace can't be fetched or that carries no Benzene span falls back to the
    /// summary plane per-row (fetch-isolation). Set the option to 0 to skip enrichment entirely.</summary>
    public async Task<IReadOnlyList<TraceSummary>> GetRecentFlowsAsync(
        int limit = 20, MeshTimeRange? range = null, CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            return Array.Empty<TraceSummary>();
        }

        var (start, end) = ResolveWindow(range, _options.RecentFlowsLookback);

        var summaries = new List<Amazon.XRay.Model.TraceSummary>();
        string? nextToken = null;
        do
        {
            var response = await _xray.GetTraceSummariesAsync(new GetTraceSummariesRequest
            {
                StartTime = start,
                EndTime = end,
                NextToken = nextToken
            }, cancellationToken);

            if (response.TraceSummaries != null)
            {
                summaries.AddRange(response.TraceSummaries);
            }

            nextToken = string.IsNullOrEmpty(response.NextToken) ? null : response.NextToken;
        }
        // Stop once we have comfortably more than the cap to sort a newest-first top-N from; the window,
        // not exhaustive paging, bounds a "recent flows" list.
        while (nextToken != null && summaries.Count < limit * 4);

        // Select the newest N by the trace-id epoch (second-granularity, but enough to pick the right ~20),
        // then enrich those rows below. Ordering within a second is refined by the enriched millisecond
        // start; the final sort applies a stable trace-id tiebreaker so both planes are deterministic.
        var top = summaries
            .OrderByDescending(s => ParseTraceStart(s.Id))
            .ThenByDescending(s => s.Id, StringComparer.Ordinal)
            .Take(limit)
            .ToList();

        var rows = await EnrichRecentFlowsAsync(top, limit, cancellationToken);

        return rows
            .OrderByDescending(t => t.StartedAt)
            .ThenByDescending(t => t.TraceId, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Enrich the chosen summaries with real span data by batching <see cref="BatchGetTracesMax"/>
    /// ids per <c>BatchGetTraces</c> call (parallel, per-batch fetch-isolation). A trace that maps to Benzene
    /// events becomes an enriched row (benzene.service names, ms start, real span count); a trace with no
    /// Benzene span or a failed batch keeps its summary-plane row. Skips all fetches when enrichment is off
    /// (<see cref="XRayTraceSourceOptions.RecentFlowsServiceEnrichmentMax"/> = 0).</summary>
    private async Task<List<TraceSummary>> EnrichRecentFlowsAsync(
        IReadOnlyList<Amazon.XRay.Model.TraceSummary> summaries, int limit, CancellationToken cancellationToken)
    {
        var enrichMax = Math.Min(Math.Max(_options.RecentFlowsServiceEnrichmentMax, 0), limit);
        if (enrichMax == 0)
        {
            return summaries.Select(ToSummaryPlaneRow).ToList();
        }

        // Which trace ids to enrich (the first enrichMax that have an id); the rest stay summary-plane.
        var toEnrich = summaries
            .Where(s => !string.IsNullOrEmpty(s.Id))
            .Take(enrichMax)
            .Select(s => s.Id!)
            .ToList();

        var mapped = new Dictionary<string, List<MeshTraceEvent>>(StringComparer.Ordinal);
        var batches = await Task.WhenAll(Chunk(toEnrich, BatchGetTracesMax).Select(FetchBatchAsync));
        foreach (var batch in batches)
        {
            foreach (var kvp in batch)
            {
                mapped[kvp.Key] = kvp.Value;
            }
        }

        // Enriched row where the trace mapped to at least one Benzene event; summary-plane fallback otherwise.
        return summaries
            .Select(s => s.Id is { } id && mapped.TryGetValue(id, out var events) && events.Count > 0
                ? EnrichedRow(s, events)
                : ToSummaryPlaneRow(s))
            .ToList();

        async Task<Dictionary<string, List<MeshTraceEvent>>> FetchBatchAsync(List<string> ids)
        {
            var result = new Dictionary<string, List<MeshTraceEvent>>(StringComparer.Ordinal);
            try
            {
                var response = await _xray.BatchGetTracesAsync(
                    new BatchGetTracesRequest { TraceIds = ids }, cancellationToken);

                foreach (var trace in response.Traces ?? new List<Trace>())
                {
                    if (trace.Segments is not { Count: > 0 } segments || string.IsNullOrEmpty(trace.Id))
                    {
                        continue;
                    }

                    result[trace.Id] = XRaySegmentMapper.Map(trace.Id, segments.Select(s => s.Document));
                }
            }
            catch
            {
                // Fetch isolation: a failed batch leaves its ≤5 rows on the summary plane, never the whole list.
            }

            return result;
        }
    }

    /// <summary>An enriched recent-flows row: real service names (benzene.service, from the mapped events),
    /// the earliest event's millisecond start, and the real span count. Keeps the summary's authoritative
    /// error flag (<c>HasError</c>/<c>HasFault</c>) and duration.</summary>
    private static TraceSummary EnrichedRow(Amazon.XRay.Model.TraceSummary summary, List<MeshTraceEvent> events) => new()
    {
        TraceId = summary.Id ?? string.Empty,
        // X-Ray Duration is in seconds; the mesh carries milliseconds.
        DurationMs = summary.Duration * 1000,
        Failed = summary.HasError || summary.HasFault,
        Services = events
            .Select(e => e.Service)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList(),
        StartedAt = events.Min(e => e.StartedAt),
        Events = events.Count,
        // The flow's entry topic: the earliest mapped event's (Map returns events in start order).
        Topic = events[0].Topic
    };

    private static TraceSummary ToSummaryPlaneRow(Amazon.XRay.Model.TraceSummary summary) => new()
    {
        TraceId = summary.Id ?? string.Empty,
        // X-Ray Duration is in seconds; the mesh carries milliseconds.
        DurationMs = summary.Duration * 1000,
        Failed = summary.HasError || summary.HasFault,
        Services = summary.ServiceIds?
            .Select(s => s.Name)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .Distinct()
            .ToList() ?? new List<string>(),
        StartedAt = ParseTraceStart(summary.Id),
        Events = 0 // X-Ray summaries carry no span count; the drill-in trace has it.
    };

    /// <summary>An X-Ray trace id is <c>1-{8 hex epoch seconds}-{24 hex}</c>; the middle group is the
    /// trace's start time. Falls back to <see cref="DateTimeOffset.MinValue"/> for an unparseable id.</summary>
    private static DateTimeOffset ParseTraceStart(string? traceId)
    {
        if (traceId is not null)
        {
            var parts = traceId.Split('-');
            if (parts.Length >= 2 && long.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var epoch))
            {
                return DateTimeOffset.FromUnixTimeSeconds(epoch);
            }
        }

        return DateTimeOffset.MinValue;
    }

    /// <summary>Fetch the given trace ids from X-Ray and map the returned segments to events under one
    /// mesh trace id (a single trace's lookup).</summary>
    private async Task<List<MeshTraceEvent>> FetchEventsAsync(
        IReadOnlyList<string> traceIds, string meshTraceId, CancellationToken cancellationToken)
    {
        var response = await _xray.BatchGetTracesAsync(
            new BatchGetTracesRequest { TraceIds = traceIds.ToList() }, cancellationToken);

        // X-Ray returns an unknown id in UnprocessedTraceIds (not Traces), so an unknown trace yields no
        // segments here → an empty event list → a null view upstream.
        var segments = (response.Traces ?? new List<Trace>())
            .Where(t => t.Segments is { Count: > 0 })
            .SelectMany(t => t.Segments)
            .Select(s => s.Document);

        return XRaySegmentMapper.Map(meshTraceId, segments);
    }

    /// <summary>Resolve a requested <see cref="MeshTimeRange"/> to X-Ray's <c>[start,end]</c> DateTimes,
    /// falling back to <c>now - <paramref name="fallback"/></c> .. <c>now</c> when no window was requested
    /// (today's behavior). X-Ray's <c>GetTraceSummaries</c> needs a bounded range either way.</summary>
    private static (DateTime Start, DateTime End) ResolveWindow(MeshTimeRange? range, TimeSpan fallback)
    {
        var resolved = MeshTimeRangeResolver.Resolve(range, DateTimeOffset.UtcNow);
        if (resolved != null)
        {
            return (resolved.Value.From.UtcDateTime, resolved.Value.To.UtcDateTime);
        }

        var end = DateTime.UtcNow;
        return (end - fallback, end);
    }

    private static DateTimeOffset EarliestStart(TraceView trace)
        => trace.Events.Count == 0 ? DateTimeOffset.MaxValue : trace.Events.Min(e => e.StartedAt);

    // X-Ray filter-expression string literals are double-quoted; escape backslashes and quotes so a
    // correlation id can't break out of the literal.
    private static string Escape(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static IEnumerable<List<string>> Chunk(IReadOnlyList<string> items, int size)
    {
        for (var i = 0; i < items.Count; i += size)
        {
            yield return items.Skip(i).Take(size).ToList();
        }
    }
}
