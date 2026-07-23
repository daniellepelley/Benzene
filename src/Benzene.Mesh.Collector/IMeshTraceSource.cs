using System.Threading;
using System.Threading.Tasks;

namespace Benzene.Mesh.Collector;

/// <summary>
/// A pluggable source of the <em>trace-shaped</em> fleet read-models — a query API over an existing
/// observability trace backend (AWS X-Ray, Grafana Tempo, Jaeger). Implemented per backend in a
/// <c>Benzene.Mesh.Fleet.*</c> adapter package; composed into an <see cref="IMeshFleetReadModel"/> by
/// <see cref="CompositeMeshFleetReadModel"/>, alongside an <c>IMeshUsageSource</c> for stats and the
/// heartbeat feed (or absent) for health — see <c>work/otel-fleet-adapter-scope.md</c>.
/// </summary>
/// <remarks>
/// Per-topic/service <em>counts</em> and service <em>health</em> are deliberately NOT on this
/// interface: traces are sampled (counts would be biased) and a trace store has no heartbeat feed.
/// Those come from their own sources. This is a trace / correlation / recent-flows reader.
/// Increments 1-3 ship <see cref="GetTraceAsync"/>, <see cref="GetCorrelationAsync"/> and
/// <see cref="GetRecentFlowsAsync"/>.
/// </remarks>
public interface IMeshTraceSource
{
    /// <summary>Fetch one flow's events (a <see cref="TraceView"/>) by trace id, or null if the backend
    /// has no such trace.</summary>
    Task<TraceView?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default);

    /// <summary>Fetch every flow that carried a business correlation id, grouped by trace (a
    /// <see cref="CorrelationView"/>), or null if none matched. A correlation id can span more than one
    /// trace, so the backend searches its traces by the correlation-id span attribute
    /// (<c>benzene.correlation-id</c>) and returns one <see cref="TraceView"/> per matching trace.</summary>
    Task<CorrelationView?> GetCorrelationAsync(string correlationId, CancellationToken cancellationToken = default);

    /// <summary>List the most recent flows (up to <paramref name="limit"/>, newest first) as
    /// <see cref="TraceSummary"/> rows for the fleet view - one per trace, carrying only per-flow facts
    /// (duration, failed, the services it touched). Deliberately a summary, not the events: a fleet load
    /// must not fan out one full trace fetch per row, and per-flow rows carry no aggregate counts (those
    /// stay the usage feed). <see cref="TraceSummary.Events"/> may be 0 when the backend's summary shape
    /// carries no span count - the accurate count is one <see cref="GetTraceAsync"/> away on drill-in.</summary>
    Task<IReadOnlyList<TraceSummary>> GetRecentFlowsAsync(int limit = 20, CancellationToken cancellationToken = default);
}
