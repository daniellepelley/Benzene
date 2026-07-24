namespace Benzene.Mesh.Fleet.Tempo;

/// <summary>
/// Where and over what windows <see cref="TempoTraceSource"/> queries Grafana Tempo's trace API. A
/// trace lookup is by id (no window); Tempo's search API (<c>GET /api/search</c>) needs a time range,
/// so correlation and recent-flows scan a bounded window.
/// </summary>
public class TempoTraceSourceOptions
{
    /// <summary>Creates the options for a Tempo base URL (e.g. <c>http://tempo:3200</c>).</summary>
    /// <param name="tempoUrl">Tempo's HTTP API base URL, without a trailing slash.</param>
    public TempoTraceSourceOptions(string tempoUrl)
    {
        TempoUrl = tempoUrl.TrimEnd('/');
    }

    /// <summary>Tempo's HTTP API base URL (the query-frontend), e.g. <c>http://tempo:3200</c>.</summary>
    public string TempoUrl { get; }

    /// <summary>How far back a <c>mesh:query:correlation</c> search scans Tempo. Default 24 hours — a
    /// business correlation id (a ticket/log id) is typically chased soon after the event.</summary>
    public TimeSpan CorrelationLookback { get; init; } = TimeSpan.FromHours(24);

    /// <summary>How far back the fleet view's recent-flows search scans Tempo. Default 1 hour — the fleet
    /// view wants the latest activity, a shorter horizon than a correlation chase.</summary>
    public TimeSpan RecentFlowsLookback { get; init; } = TimeSpan.FromHours(1);
}
