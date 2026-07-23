using System.Threading;
using System.Threading.Tasks;

namespace Benzene.Mesh.Collector;

/// <summary>
/// An <see cref="IMeshFleetReadModel"/> backed by a pluggable <see cref="IMeshTraceSource"/> (X-Ray /
/// Tempo / Jaeger) — the backend-composed alternative to the in-memory push-collector. Increments 1-2
/// serve <c>mesh:query:trace</c> and <c>mesh:query:correlation</c> from the trace source; recent-flows
/// and the usage-fed stats compose in a later increment (see <c>work/otel-fleet-adapter-scope.md</c>).
/// </summary>
/// <remarks>
/// Until that increment lands, the remaining read-models return their honest empty/absent shapes: an
/// empty <see cref="FleetView"/> (no services/topics observed through this reader yet) and null for
/// service/topic. A trace-only deployment therefore answers trace and correlation lookups and reports
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

    public Task<CorrelationView?> CorrelationAsync(string correlationId, CancellationToken cancellationToken = default)
        => _traceSource.GetCorrelationAsync(correlationId, cancellationToken);

    // Composed in a later increment (fleet recent-flows + usage-fed stats = inc 3).
    public Task<FleetView> FleetAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new FleetView());

    public Task<ServiceView?> ServiceAsync(string name, CancellationToken cancellationToken = default)
        => Task.FromResult<ServiceView?>(null);

    public Task<TopicSummary?> TopicAsync(string id, string? version, CancellationToken cancellationToken = default)
        => Task.FromResult<TopicSummary?>(null);
}
