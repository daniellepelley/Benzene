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
    /// Registers an <see cref="XRayTraceSource"/> as the <see cref="IMeshTraceSource"/> and composes it
    /// into an <see cref="IMeshFleetReadModel"/> (<see cref="TraceSourceFleetReadModel"/>), so the
    /// <c>mesh:query:trace</c> handler answers from X-Ray. Wire the read side with
    /// <c>UseMessageHandlers(MeshCollectorHandlers.Queries)</c> and the fleet UI with
    /// <c>UseMeshFleetUi()</c>; no <see cref="MeshCollectorStore"/> is needed - there is no push
    /// ingestion, only queries against X-Ray.
    /// </summary>
    /// <remarks>
    /// Registers a default <see cref="IAmazonXRay"/> (resolving region and credentials from the ambient
    /// AWS environment - on Lambda, the execution role) unless one is already registered, the same
    /// pattern as <c>AddCloudWatchUsage</c>. Only the trace read model is X-Ray-backed in this
    /// increment; service/topic/correlation and fleet stats return their honest empty/absent shapes
    /// until the composing increments land.
    /// </remarks>
    /// <param name="services">The service container to register with.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddXRayFleetReadModel(this IBenzeneServiceContainer services)
    {
        services.AddSingleton<IAmazonXRay>(_ => new AmazonXRayClient());
        services.AddSingleton<IMeshTraceSource, XRayTraceSource>();
        services.AddSingleton<IMeshFleetReadModel, TraceSourceFleetReadModel>();
        return services;
    }
}
