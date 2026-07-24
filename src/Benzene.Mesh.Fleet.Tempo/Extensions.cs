using Benzene.Abstractions.DI;
using Benzene.Mesh.Collector;

namespace Benzene.Mesh.Fleet.Tempo;

/// <summary>
/// Registers the Tempo-backed fleet reader: the fleet view served from Grafana Tempo's trace API
/// instead of the in-memory push collector. See <c>work/otel-fleet-adapter-scope.md</c>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers a <see cref="TempoTraceSource"/> as the <see cref="IMeshTraceSource"/> and composes it —
    /// with whatever <c>IMeshUsageSource</c>s are registered — into a
    /// <see cref="CompositeMeshFleetReadModel"/> serving <see cref="IMeshFleetReadModel"/>, so the whole
    /// fleet view (trace + correlation + recent flows from Tempo, topic stats from a usage feed if one is
    /// wired) answers off Tempo. Wire the read side with
    /// <c>UseMessageHandlers(MeshCollectorHandlers.Queries)</c> and point the mesh UI's live Fleet plane at
    /// it with <c>UseMeshUi(..., envelopeUrl: "/benzene/invoke")</c>; no <see cref="MeshCollectorStore"/> is
    /// needed — there is no push ingestion.
    /// </summary>
    /// <remarks>
    /// Registers an <see cref="HttpClient"/> unless one is already registered (the same shape as
    /// <c>Benzene.Mesh.Tracing.Tempo.AddTempoTopology</c>). Add a usage source separately for topic stats;
    /// without one the composite still serves traces/correlation/recent-flows/services and reports no topic
    /// stats (honest empty). Per-service and single-topic pages stay omitted (no descriptor feed here).
    /// </remarks>
    /// <param name="services">The service container to register with.</param>
    /// <param name="options">Where and over what windows to query Tempo's trace API.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddTempoFleetReadModel(
        this IBenzeneServiceContainer services, TempoTraceSourceOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IMeshTraceSource, TempoTraceSource>();
        services.AddSingleton<IMeshFleetReadModel, CompositeMeshFleetReadModel>();
        return services;
    }
}
