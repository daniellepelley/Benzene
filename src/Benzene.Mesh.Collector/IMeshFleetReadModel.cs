using System.Threading;
using System.Threading.Tasks;

namespace Benzene.Mesh.Collector;

/// <summary>
/// The read side of the fleet the <c>mesh:query:*</c> handlers answer — the seam that makes the fleet
/// UI's data source swappable. The in-memory <see cref="MeshCollectorStore"/> is one implementation
/// (push-collector plane); a backend-composed implementation
/// (<see cref="CompositeMeshFleetReadModel"/> / <c>Benzene.Mesh.Fleet.*</c>) reads traces from an OTel
/// trace store and stats from an <c>IMeshUsageSource</c> — see <c>work/otel-fleet-adapter-scope.md</c>.
/// Async because a backend-composed reader does I/O; the in-memory store just wraps its sync methods.
/// </summary>
public interface IMeshFleetReadModel
{
    /// <summary>The whole known fleet — services, topic catalog, recent flows (<c>mesh:query:fleet</c>).
    /// An optional <paramref name="range"/> windows the recent flows (and, where the plane can, the counts);
    /// null ⇒ unfiltered, today's behavior. The returned <see cref="FleetView.Window"/> reports what was
    /// applied and whether counts honored it.</summary>
    Task<FleetView> FleetAsync(MeshTimeRange? range = null, CancellationToken cancellationToken = default);

    /// <summary>One service's detail, or null if unknown (<c>mesh:query:service</c>). Optional
    /// <paramref name="range"/> windows the service's live flows; null ⇒ unfiltered.</summary>
    Task<ServiceView?> ServiceAsync(string name, MeshTimeRange? range = null, CancellationToken cancellationToken = default);

    /// <summary>One topic's summary, or null if unknown (<c>mesh:query:topic</c>). Optional
    /// <paramref name="range"/> windows the topic's live flows; null ⇒ unfiltered.</summary>
    Task<TopicSummary?> TopicAsync(string id, string? version, MeshTimeRange? range = null, CancellationToken cancellationToken = default);

    /// <summary>One flow's events in start order → the waterfall, or null if unknown (<c>mesh:query:trace</c>).
    /// A trace lookup is by id and takes no window.</summary>
    Task<TraceView?> TraceAsync(string traceId, CancellationToken cancellationToken = default);

    /// <summary>Every flow that carried a business correlation id, or null if none (<c>mesh:query:correlation</c>).
    /// Optional <paramref name="range"/> windows the search; null ⇒ unfiltered.</summary>
    Task<CorrelationView?> CorrelationAsync(string correlationId, MeshTimeRange? range = null, CancellationToken cancellationToken = default);
}
