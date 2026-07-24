using Benzene.Abstractions.DI;
using Benzene.Mesh.Collector;

namespace Benzene.Mesh.Fleet.Jaeger;

/// <summary>
/// Registers the Jaeger-backed fleet reader: the fleet view served from a Jaeger query service instead
/// of the in-memory push collector. See <c>work/otel-fleet-adapter-scope.md</c>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers a <see cref="JaegerTraceSource"/> as the <see cref="IMeshTraceSource"/> and composes it —
    /// with whatever <c>IMeshUsageSource</c>s are registered — into a
    /// <see cref="CompositeMeshFleetReadModel"/> serving <see cref="IMeshFleetReadModel"/>. Wire the read
    /// side with <c>UseMessageHandlers(MeshCollectorHandlers.Queries)</c> and point the mesh UI's live Fleet
    /// plane at it with <c>UseMeshUi(..., envelopeUrl: "/benzene/invoke")</c>; no
    /// <see cref="MeshCollectorStore"/> is needed — there is no push ingestion.
    /// </summary>
    /// <remarks>
    /// Registers an <see cref="HttpClient"/> unless one is already registered. Add a usage source
    /// separately for topic stats; without one the composite still serves traces/correlation/recent-flows/
    /// services and reports no topic stats (honest empty). Per-service and single-topic pages stay omitted
    /// (no descriptor feed here).
    /// </remarks>
    /// <param name="services">The service container to register with.</param>
    /// <param name="options">Where and over what windows to query Jaeger.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddJaegerFleetReadModel(
        this IBenzeneServiceContainer services, JaegerTraceSourceOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IMeshTraceSource, JaegerTraceSource>();
        services.AddSingleton<IMeshFleetReadModel, CompositeMeshFleetReadModel>();
        return services;
    }
}
