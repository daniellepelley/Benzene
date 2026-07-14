namespace Benzene.Mesh.Contracts;

/// <summary>
/// One cross-service call edge in a <see cref="MeshTopology"/> - "<see cref="Client"/> calls
/// <see cref="Server"/>", from a given <see cref="TopologyEdgeSource"/>.
/// </summary>
/// <remarks>
/// Structural and observed edges for the same <see cref="Client"/>/<see cref="Server"/> pair are
/// kept as separate entries rather than merged, so a UI can show "designed to call" vs. "actually
/// calling, and how it's performing" distinctly - see
/// <c>work/service-mesh-roadmap-1.0.md</c> §7.3.
/// </remarks>
public class TopologyEdge
{
    /// <summary>Initializes a new instance of the <see cref="TopologyEdge"/> class.</summary>
    /// <param name="client">The name of the calling service.</param>
    /// <param name="server">The name of the called service.</param>
    /// <param name="source">One of <see cref="TopologyEdgeSource"/>.</param>
    /// <param name="requestsPerMinute">Observed call rate, or <c>null</c> if not available for this <paramref name="source"/>.</param>
    /// <param name="errorRate">Observed failure ratio (0-1), or <c>null</c> if not available for this <paramref name="source"/>.</param>
    /// <param name="p50LatencyMs">Observed median latency in milliseconds, or <c>null</c> if not available.</param>
    /// <param name="p95LatencyMs">Observed 95th-percentile latency in milliseconds, or <c>null</c> if not available.</param>
    /// <param name="p99LatencyMs">Observed 99th-percentile latency in milliseconds, or <c>null</c> if not available.</param>
    public TopologyEdge(
        string client,
        string server,
        string source,
        double? requestsPerMinute,
        double? errorRate,
        double? p50LatencyMs,
        double? p95LatencyMs,
        double? p99LatencyMs)
    {
        Client = client;
        Server = server;
        Source = source;
        RequestsPerMinute = requestsPerMinute;
        ErrorRate = errorRate;
        P50LatencyMs = p50LatencyMs;
        P95LatencyMs = p95LatencyMs;
        P99LatencyMs = p99LatencyMs;
    }

    /// <summary>The name of the calling service.</summary>
    public string Client { get; }

    /// <summary>The name of the called service.</summary>
    public string Server { get; }

    /// <summary>One of <see cref="TopologyEdgeSource"/>.</summary>
    public string Source { get; }

    /// <summary>Observed call rate, or <c>null</c> if not available for this <see cref="Source"/>.</summary>
    public double? RequestsPerMinute { get; }

    /// <summary>Observed failure ratio (0-1), or <c>null</c> if not available for this <see cref="Source"/>.</summary>
    public double? ErrorRate { get; }

    /// <summary>Observed median latency in milliseconds, or <c>null</c> if not available.</summary>
    public double? P50LatencyMs { get; }

    /// <summary>Observed 95th-percentile latency in milliseconds, or <c>null</c> if not available.</summary>
    public double? P95LatencyMs { get; }

    /// <summary>Observed 99th-percentile latency in milliseconds, or <c>null</c> if not available.</summary>
    public double? P99LatencyMs { get; }
}
