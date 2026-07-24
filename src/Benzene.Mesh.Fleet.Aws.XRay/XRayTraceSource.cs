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
    /// recent-flows window (no filter), mapped to <see cref="TraceSummary"/> rows, newest first, capped at
    /// <paramref name="limit"/>. Zero <c>BatchGetTraces</c> — the fleet load must not fan out per row; the
    /// summary carries no span count, so <see cref="TraceSummary.Events"/> is 0 (the accurate count is one
    /// <c>GetTraceAsync</c> away on drill-in).</summary>
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

        return summaries
            .Select(ToTraceSummary)
            .OrderByDescending(t => t.StartedAt)
            .Take(limit)
            .ToList();
    }

    private static TraceSummary ToTraceSummary(Amazon.XRay.Model.TraceSummary summary) => new()
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
