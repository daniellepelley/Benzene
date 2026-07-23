using System.Threading;
using System.Threading.Tasks;

namespace Benzene.Mesh.Collector;

/// <summary>
/// A pluggable source of the <em>trace-shaped</em> fleet read-models — a query API over an existing
/// observability trace backend (AWS X-Ray, Grafana Tempo, Jaeger). Implemented per backend in a
/// <c>Benzene.Mesh.Fleet.*</c> adapter package; composed into an <see cref="IMeshFleetReadModel"/> by
/// <see cref="TraceSourceFleetReadModel"/>, alongside an <c>IMeshUsageSource</c> for stats and the
/// heartbeat feed (or absent) for health — see <c>work/otel-fleet-adapter-scope.md</c>.
/// </summary>
/// <remarks>
/// Per-topic/service <em>counts</em> and service <em>health</em> are deliberately NOT on this
/// interface: traces are sampled (counts would be biased) and a trace store has no heartbeat feed.
/// Those come from their own sources. This is a trace / correlation / recent-flows reader.
/// Correlation and recent-flows are added in later increments; increment 1 ships <see cref="GetTraceAsync"/>.
/// </remarks>
public interface IMeshTraceSource
{
    /// <summary>Fetch one flow's events (a <see cref="TraceView"/>) by trace id, or null if the backend
    /// has no such trace.</summary>
    Task<TraceView?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default);
}
