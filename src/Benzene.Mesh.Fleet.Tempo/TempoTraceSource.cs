using System.Globalization;
using System.Text.Json;
using Benzene.Mesh.Collector;
using Benzene.Mesh.Wire;

namespace Benzene.Mesh.Fleet.Tempo;

/// <summary>
/// An <see cref="IMeshTraceSource"/> that answers the fleet's trace read-models from Grafana Tempo's
/// HTTP trace API: a trace by id (<c>GET /api/traces/{id}</c>), a correlation-id search + group
/// (<c>GET /api/search</c> with TraceQL), and the recent-flows list (an unfiltered-by-id TraceQL search).
/// The non-AWS reference realisation of the trace-backed fleet reader scoped in
/// <c>work/otel-fleet-adapter-scope.md</c> — it reuses the same <see cref="CompositeMeshFleetReadModel"/>,
/// handlers, and UI as the X-Ray adapter, differing only in the backend it reads.
/// </summary>
/// <remarks>
/// Unlike the topology adapter (<c>Benzene.Mesh.Tracing.Tempo</c>, which queries Tempo's
/// metrics-generator via PromQL), this reads Tempo's <em>trace</em> API directly. Trace stats and
/// service health are deliberately not sourced here (see <see cref="IMeshTraceSource"/>): traces are
/// sampled and Tempo has no heartbeat feed. Following the topology adapter's philosophy, a
/// reachable-but-unsuccessful response (HTTP error, or an unexpected/empty body) surfaces as
/// null/empty rather than throwing; a genuine connection failure still throws (and the composite's
/// fetch-isolation degrades that slice).
/// </remarks>
public class TempoTraceSource : IMeshTraceSource
{
    private readonly HttpClient _httpClient;
    private readonly TempoTraceSourceOptions _options;

    /// <summary>Creates the source over an <see cref="HttpClient"/> and Tempo endpoint/window options.</summary>
    public TempoTraceSource(HttpClient httpClient, TempoTraceSourceOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<TraceView?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(traceId))
        {
            return null;
        }

        var events = await FetchTraceEventsAsync(traceId, cancellationToken);
        // Null (not empty) when Tempo has no such trace, or a real trace carried no Benzene topic-bearing
        // span — so the query handler answers NotFound, not an empty waterfall.
        return events.Count == 0 ? null : new TraceView { TraceId = traceId, Events = events };
    }

    public async Task<CorrelationView?> GetCorrelationAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(correlationId))
        {
            return null;
        }

        // Attribute names carry dots and a hyphen, so quote the name in TraceQL.
        var traceQl = $"{{ span.\"benzene.correlation-id\" = \"{Escape(correlationId)}\" }}";
        var matches = await SearchAsync(traceQl, _options.CorrelationLookback, limit: 100, cancellationToken);
        if (matches.Count == 0)
        {
            return null;
        }

        var traces = new List<TraceView>();
        foreach (var match in matches)
        {
            var events = await FetchTraceEventsAsync(match.TraceId, cancellationToken);
            if (events.Count > 0)
            {
                traces.Add(new TraceView { TraceId = match.TraceId, Events = events });
            }
        }

        if (traces.Count == 0)
        {
            return null;
        }

        // Earliest-first, the same ordering the in-memory collector and the X-Ray adapter use so the UI
        // renders every plane's correlation view identically.
        traces.Sort((a, b) => EarliestStart(a).CompareTo(EarliestStart(b)));
        return new CorrelationView { CorrelationId = correlationId, Traces = traces };
    }

    public async Task<IReadOnlyList<TraceSummary>> GetRecentFlowsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            return Array.Empty<TraceSummary>();
        }

        // Any trace carrying a Benzene topic is a mesh flow; this filters out non-mesh traces the same way
        // the span→event mapper does, so the fleet's recent-flows list is mesh flows only.
        var matches = await SearchAsync("{ span.\"benzene.topic\" != \"\" }", _options.RecentFlowsLookback, limit, cancellationToken);

        return matches
            .Select(m => new TraceSummary
            {
                TraceId = m.TraceId,
                DurationMs = m.DurationMs,
                StartedAt = m.StartedAt,
                Services = string.IsNullOrEmpty(m.RootServiceName) ? new List<string>() : new List<string> { m.RootServiceName! },
                // Tempo's search summary carries no aggregate error flag and no span count; the failure
                // colouring and accurate event count come from the drill-in trace (GetTraceAsync).
                Failed = false,
                Events = 0
            })
            .OrderByDescending(t => t.StartedAt)
            .Take(limit)
            .ToList();
    }

    private async Task<List<MeshTraceEvent>> FetchTraceEventsAsync(string traceId, CancellationToken cancellationToken)
    {
        var body = await GetStringOrNullAsync($"{_options.TempoUrl}/api/traces/{Uri.EscapeDataString(traceId)}", cancellationToken);
        return body is null ? new List<MeshTraceEvent>() : TempoTraceMapper.Map(traceId, body);
    }

    private async Task<List<TempoTraceMatch>> SearchAsync(
        string traceQl, TimeSpan lookback, int limit, CancellationToken cancellationToken)
    {
        var end = DateTimeOffset.UtcNow;
        var start = end - lookback;
        var url = $"{_options.TempoUrl}/api/search?q={Uri.EscapeDataString(traceQl)}"
                  + $"&start={start.ToUnixTimeSeconds()}&end={end.ToUnixTimeSeconds()}&limit={limit}";

        var body = await GetStringOrNullAsync(url, cancellationToken);
        return body is null ? new List<TempoTraceMatch>() : ParseSearch(body);
    }

    /// <summary>GET a URL, returning the body on success or null on any reachable-but-unsuccessful
    /// response (HTTP error) — the topology adapter's "one bad query shouldn't fault the build" rule. A
    /// connection-level failure still throws (the composite's fetch-isolation catches it).</summary>
    private async Task<string?> GetStringOrNullAsync(string url, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static List<TempoTraceMatch> ParseSearch(string body)
    {
        var matches = new List<TempoTraceMatch>();

        JsonDocument parsed;
        try
        {
            parsed = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return matches;
        }

        using (parsed)
        {
            if (!parsed.RootElement.TryGetProperty("traces", out var traces) || traces.ValueKind != JsonValueKind.Array)
            {
                return matches;
            }

            foreach (var trace in traces.EnumerateArray())
            {
                var traceId = GetString(trace, "traceID") ?? GetString(trace, "traceId");
                if (string.IsNullOrEmpty(traceId))
                {
                    continue;
                }

                matches.Add(new TempoTraceMatch(
                    traceId!,
                    NanosToTime(GetString(trace, "startTimeUnixNano")),
                    GetDouble(trace, "durationMs"),
                    GetString(trace, "rootServiceName")));
            }
        }

        return matches;
    }

    private static DateTimeOffset EarliestStart(TraceView trace)
        => trace.Events.Count == 0 ? DateTimeOffset.MaxValue : trace.Events.Min(e => e.StartedAt);

    // TraceQL string literals are double-quoted; escape backslashes and quotes so a value can't break out.
    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static DateTimeOffset NanosToTime(string? nanos)
        => long.TryParse(nanos, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(value / 1_000_000)
            : default;

    private static string? GetString(JsonElement node, string name)
        => node.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static double GetDouble(JsonElement node, string name)
    {
        if (node.TryGetProperty(name, out var element))
        {
            if (element.ValueKind == JsonValueKind.Number)
            {
                return element.GetDouble();
            }
            if (element.ValueKind == JsonValueKind.String
                && double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
        }

        return 0;
    }

    private readonly record struct TempoTraceMatch(string TraceId, DateTimeOffset StartedAt, double DurationMs, string? RootServiceName);
}
