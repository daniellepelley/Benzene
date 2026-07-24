using System.Text.Json;
using Benzene.Mesh.Collector;
using Benzene.Mesh.Wire;

namespace Benzene.Mesh.Fleet.Jaeger;

/// <summary>
/// An <see cref="IMeshTraceSource"/> that answers the fleet's trace read-models from a Jaeger query
/// service: a trace by id (<c>GET /api/traces/{id}</c>), a correlation-id tag search + group, and the
/// recent-flows list. A second non-AWS reference realisation alongside <c>Benzene.Mesh.Fleet.Tempo</c>,
/// reusing the same <see cref="CompositeMeshFleetReadModel"/>, handlers, and UI — see
/// <c>work/otel-fleet-adapter-scope.md</c>.
/// </summary>
/// <remarks>
/// Jaeger's search API requires a <c>service</c> (there is no "all services" query), so correlation and
/// recent-flows fan the search out across services — the configured <see cref="JaegerTraceSourceOptions.Services"/>
/// or, when unset, the ones discovered via <c>GET /api/services</c>. Jaeger search returns <b>full</b>
/// traces (not summaries), so — unlike the Tempo adapter — recent flows carry a real span count and
/// failure flag without a second fetch. A reachable-but-unsuccessful response surfaces as null/empty (the
/// topology adapter's rule); a genuine connection failure throws and the composite's fetch-isolation
/// degrades that slice.
/// </remarks>
public class JaegerTraceSource : IMeshTraceSource
{
    private readonly HttpClient _httpClient;
    private readonly JaegerTraceSourceOptions _options;

    /// <summary>Creates the source over an <see cref="HttpClient"/> and Jaeger endpoint/window options.</summary>
    public JaegerTraceSource(HttpClient httpClient, JaegerTraceSourceOptions options)
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

        var body = await GetStringOrNullAsync($"{_options.JaegerUrl}/api/traces/{Uri.EscapeDataString(traceId)}", cancellationToken);
        if (body is null)
        {
            return null;
        }

        var traces = JaegerTraceMapper.MapTraces(body);
        var match = traces.FirstOrDefault(t => t.TraceId == traceId);
        var events = match.Events ?? traces.FirstOrDefault().Events;

        // Null (not empty) when Jaeger has no such trace, or it carried no Benzene topic-bearing span — so
        // the query handler answers NotFound, not an empty waterfall.
        return events is not { Count: > 0 } ? null : new TraceView { TraceId = traceId, Events = events };
    }

    public async Task<CorrelationView?> GetCorrelationAsync(string correlationId, MeshTimeRange? range = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(correlationId))
        {
            return null;
        }

        var tags = $"{{\"benzene.correlation-id\":\"{Escape(correlationId)}\"}}";
        var traces = await SearchAcrossServicesAsync(ResolveWindow(range, _options.CorrelationLookback), tags, cancellationToken);

        var views = traces
            .Where(t => t.Events.Count > 0)
            .Select(t => new TraceView { TraceId = t.TraceId, Events = t.Events })
            .OrderBy(EarliestStart) // earliest-first, matching every other plane's correlation view
            .ToList();

        return views.Count == 0 ? null : new CorrelationView { CorrelationId = correlationId, Traces = views };
    }

    public async Task<IReadOnlyList<TraceSummary>> GetRecentFlowsAsync(int limit = 20, MeshTimeRange? range = null, CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            return Array.Empty<TraceSummary>();
        }

        var traces = await SearchAcrossServicesAsync(ResolveWindow(range, _options.RecentFlowsLookback), tags: null, cancellationToken);

        return traces
            .Where(t => t.Events.Count > 0) // mesh flows only (a trace with no Benzene span isn't one)
            .Select(ToTraceSummary)
            .OrderByDescending(t => t.StartedAt)
            .Take(limit)
            .ToList();
    }

    /// <summary>Fan a search out across every service (Jaeger requires one per query), merge the returned
    /// full traces, and dedupe by trace id (a cross-service trace is returned by each of its services).</summary>
    private async Task<List<JaegerMappedTrace>> SearchAcrossServicesAsync(
        (DateTimeOffset Start, DateTimeOffset End) window, string? tags, CancellationToken cancellationToken)
    {
        var services = await GetServicesAsync(cancellationToken);
        if (services.Count == 0)
        {
            return new List<JaegerMappedTrace>();
        }

        var startMicros = window.Start.ToUnixTimeMilliseconds() * 1000;
        var endMicros = window.End.ToUnixTimeMilliseconds() * 1000;

        var byTraceId = new Dictionary<string, JaegerMappedTrace>();
        foreach (var service in services)
        {
            var url = $"{_options.JaegerUrl}/api/traces?service={Uri.EscapeDataString(service)}"
                      + $"&start={startMicros}&end={endMicros}&limit={_options.SearchLimitPerService}";
            if (tags is not null)
            {
                url += $"&tags={Uri.EscapeDataString(tags)}";
            }

            var body = await GetStringOrNullAsync(url, cancellationToken);
            if (body is null)
            {
                continue;
            }

            foreach (var trace in JaegerTraceMapper.MapTraces(body))
            {
                byTraceId.TryAdd(trace.TraceId, trace);
            }
        }

        return byTraceId.Values.ToList();
    }

    /// <summary>The services to search: the configured set, or those discovered via <c>/api/services</c>.</summary>
    private async Task<IReadOnlyList<string>> GetServicesAsync(CancellationToken cancellationToken)
    {
        if (_options.Services is { Count: > 0 } configured)
        {
            return configured;
        }

        var body = await GetStringOrNullAsync($"{_options.JaegerUrl}/api/services", cancellationToken);
        if (body is null)
        {
            return Array.Empty<string>();
        }

        try
        {
            using var parsed = JsonDocument.Parse(body);
            if (parsed.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                return data.EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString()!)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();
            }
        }
        catch (JsonException)
        {
            // Malformed body → no services, same reachable-but-unusable degradation as an HTTP error.
        }

        return Array.Empty<string>();
    }

    /// <summary>Jaeger search returns full traces, so a recent-flow row's span count, touched services, and
    /// failure flag come straight from the mapped events — richer than a summary-only backend.</summary>
    private static TraceSummary ToTraceSummary(JaegerMappedTrace trace)
    {
        var startedAt = trace.Events.Min(e => e.StartedAt);
        var end = trace.Events.Max(e => e.StartedAt + TimeSpan.FromMilliseconds(e.DurationMs));
        return new TraceSummary
        {
            TraceId = trace.TraceId,
            Events = trace.Events.Count,
            Services = trace.Events.Where(e => !string.IsNullOrEmpty(e.Service))
                .Select(e => e.Service!).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList(),
            StartedAt = startedAt,
            DurationMs = (end - startedAt).TotalMilliseconds,
            // Same success class the in-memory collector's trace summaries use (unknown/empty = failure).
            Failed = trace.Events.Any(e => !BenzeneResultStatusExtensions.IsSuccess(e.Status))
        };
    }

    /// <summary>GET a URL, returning the body on success or null on any reachable-but-unsuccessful
    /// response — the topology adapter's "one bad query shouldn't fault the build" rule. A connection-level
    /// failure still throws (the composite's fetch-isolation catches it).</summary>
    private async Task<string?> GetStringOrNullAsync(string url, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(url, cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync(cancellationToken) : null;
    }

    /// <summary>Resolve a requested <see cref="MeshTimeRange"/> to Jaeger's search <c>[start,end]</c>, falling
    /// back to <c>now - <paramref name="fallback"/></c> .. <c>now</c> when no window was requested (today's
    /// behavior). Jaeger's search needs a bounded range either way.</summary>
    private static (DateTimeOffset Start, DateTimeOffset End) ResolveWindow(MeshTimeRange? range, TimeSpan fallback)
    {
        var resolved = MeshTimeRangeResolver.Resolve(range, DateTimeOffset.UtcNow);
        if (resolved != null)
        {
            return (resolved.Value.From, resolved.Value.To);
        }

        var end = DateTimeOffset.UtcNow;
        return (end - fallback, end);
    }

    private static DateTimeOffset EarliestStart(TraceView trace)
        => trace.Events.Count == 0 ? DateTimeOffset.MaxValue : trace.Events.Min(e => e.StartedAt);

    // Jaeger's tags filter is a JSON object string; escape the value so it can't break out of the literal.
    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
