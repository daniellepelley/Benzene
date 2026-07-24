using Amazon.XRay;
using Benzene.Abstractions.DI;
using Benzene.Mesh.Collector;

namespace Benzene.Mesh.Fleet.Aws.XRay;

/// <summary>
/// Registers the X-Ray-backed fleet reader: the fleet UI's trace waterfall served from AWS X-Ray
/// instead of the in-memory push collector. See <c>work/otel-fleet-adapter-scope.md</c>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers an <see cref="XRayTraceSource"/> as the <see cref="IMeshTraceSource"/> and composes it —
    /// with whatever <c>IMeshUsageSource</c>s are registered (e.g. <c>AddCloudWatchUsage</c>) — into a
    /// <see cref="CompositeMeshFleetReadModel"/> serving <see cref="IMeshFleetReadModel"/>, so the whole
    /// fleet view (trace + correlation from X-Ray, topic stats from the usage feed, recent flows +
    /// anonymous services from X-Ray) answers on the AWS plane. Wire the read side with
    /// <c>UseMessageHandlers(MeshCollectorHandlers.Queries)</c> and point the mesh UI's live Fleet plane
    /// at it with <c>UseMeshUi(..., envelopeUrl: "/benzene/invoke")</c>; no <see cref="MeshCollectorStore"/>
    /// is needed - there is no push ingestion, only queries against X-Ray + the usage backend.
    /// </summary>
    /// <remarks>
    /// Registers a default <see cref="IAmazonXRay"/> (resolving region and credentials from the ambient
    /// AWS environment - on Lambda, the execution role) unless one is already registered, the same
    /// pattern as <c>AddCloudWatchUsage</c>. Add a usage source separately (<c>AddCloudWatchUsage</c>) for
    /// topic stats; without one the composite still serves traces/correlation/recent-flows/services and
    /// reports no topic stats (honest empty), rather than fabricating counts. Per-service and single-topic
    /// pages stay omitted (there is no descriptor feed on this plane).
    /// </remarks>
    /// <param name="services">The service container to register with.</param>
    /// <param name="options">Correlation + recent-flows tuning (lookback windows); defaults if omitted.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddXRayFleetReadModel(
        this IBenzeneServiceContainer services, XRayTraceSourceOptions? options = null)
    {
        services.AddSingleton(options ?? new XRayTraceSourceOptions());
        services.AddSingleton<IAmazonXRay>(_ => new AmazonXRayClient());
        services.AddSingleton<IMeshTraceSource, XRayTraceSource>();
        services.AddSingleton<IMeshFleetReadModel, CompositeMeshFleetReadModel>();
        return services;
    }
}
