namespace Benzene.Mesh.Fleet.Jaeger;

/// <summary>
/// Where and over what windows <see cref="JaegerTraceSource"/> queries a Jaeger query service. A trace
/// lookup is by id (no window); Jaeger's search API needs a time range and — unlike Tempo/X-Ray — a
/// <em>service</em>, so correlation and recent-flows fan a bounded search out across services.
/// </summary>
public class JaegerTraceSourceOptions
{
    /// <summary>Creates the options for a Jaeger query base URL (e.g. <c>http://jaeger:16686</c>).</summary>
    /// <param name="jaegerUrl">Jaeger query's HTTP base URL, without a trailing slash.</param>
    public JaegerTraceSourceOptions(string jaegerUrl)
    {
        JaegerUrl = jaegerUrl.TrimEnd('/');
    }

    /// <summary>Jaeger query's HTTP API base URL, e.g. <c>http://jaeger:16686</c>.</summary>
    public string JaegerUrl { get; }

    /// <summary>The services to fan a correlation / recent-flows search across. Jaeger's search API
    /// requires a <c>service</c> parameter (there is no "all services" query), so a fleet-wide search
    /// must enumerate services. When null/empty, the source discovers them via <c>GET /api/services</c>
    /// each time; set this to pin the set (fewer calls, or to scope to the mesh's own services).</summary>
    public IReadOnlyList<string>? Services { get; init; }

    /// <summary>How far back a <c>mesh:query:correlation</c> search scans Jaeger. Default 24 hours.</summary>
    public TimeSpan CorrelationLookback { get; init; } = TimeSpan.FromHours(24);

    /// <summary>How far back the fleet view's recent-flows search scans Jaeger. Default 1 hour.</summary>
    public TimeSpan RecentFlowsLookback { get; init; } = TimeSpan.FromHours(1);

    /// <summary>Jaeger search <c>limit</c> per service call — the max traces one service's search returns
    /// before the results are merged and re-capped. Default 20.</summary>
    public int SearchLimitPerService { get; init; } = 20;
}
