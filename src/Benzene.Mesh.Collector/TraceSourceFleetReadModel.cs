using System.Threading;
using System.Threading.Tasks;

namespace Benzene.Mesh.Collector;

/// <summary>
/// An <see cref="IMeshFleetReadModel"/> backed by a pluggable <see cref="IMeshTraceSource"/> (X-Ray /
/// Tempo / Jaeger) — the backend-composed alternative to the in-memory push-collector. Increment 1
/// serves <c>mesh:query:trace</c> from the trace source; correlation, recent-flows, and the
/// usage-fed stats compose in later increments (see <c>work/otel-fleet-adapter-scope.md</c>).
/// </summary>
/// <remarks>
/// Until those increments land, the non-trace read-models return their honest empty/absent shapes: an
/// empty <see cref="FleetView"/> (no services/topics observed through this reader yet) and null for
/// service/topic/correlation. A trace-only deployment therefore answers trace lookups and reports
/// "nothing else known here" rather than fabricating a fleet — the mesh degradation rule.
/// </remarks>
public class TraceSourceFleetReadModel : IMeshFleetReadModel
{
    private readonly IMeshTraceSource _traceSource;

    public TraceSourceFleetReadModel(IMeshTraceSource traceSource)
    {
        _traceSource = traceSource;
    }

    public Task<TraceView?> TraceAsync(string traceId, CancellationToken cancellationToken = default)
        => _traceSource.GetTraceAsync(traceId, cancellationToken);

    // Composed in later increments (correlation = inc 2; fleet recent-flows + usage-fed stats = inc 3).
    public Task<CorrelationView?> CorrelationAsync(string correlationId, CancellationToken cancellationToken = default)
        => Task.FromResult<CorrelationView?>(null);

    public Task<FleetView> FleetAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new FleetView());

    public Task<ServiceView?> ServiceAsync(string name, CancellationToken cancellationToken = default)
        => Task.FromResult<ServiceView?>(null);

    public Task<TopicSummary?> TopicAsync(string id, string? version, CancellationToken cancellationToken = default)
        => Task.FromResult<TopicSummary?>(null);
}
