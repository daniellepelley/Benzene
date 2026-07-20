using Grpc.AspNetCore.Server;

namespace Benzene.Grpc.AspNet;

/// <summary>
/// Options for <c>AddBenzeneGrpc</c>. Health checks and reflection are both off by default - each is a
/// small amount of extra surface area (grpc.health.v1, grpc.reflection.v1alpha) that a service should opt
/// into deliberately rather than get for free.
/// </summary>
public class BenzeneGrpcOptions
{
    /// <summary>
    /// When <c>true</c>, registers ASP.NET Core's gRPC health check services and bridges Benzene's own
    /// health checks (<see cref="Benzene.HealthChecks.Core.IHealthCheck"/>) onto them via
    /// <see cref="BenzeneHealthCheckBridge"/>, so grpc.health.v1's <c>Check</c>/<c>Watch</c> reflect
    /// Benzene health. Map the service with <c>MapBenzeneGrpcHealthService</c>.
    /// </summary>
    public bool EnableHealthChecks { get; set; }

    /// <summary>
    /// When set (and <see cref="EnableHealthChecks"/> is on), the named grpc.health.v1 service
    /// <c>"liveness"</c> reports only the Benzene checks whose <c>Type</c> is in this list, so a gRPC
    /// liveness probe can be scoped to cheap/local checks (mirrors HTTP <c>UseLivenessCheck</c>). Null
    /// (the default) publishes no separate liveness service - only the overall <c>""</c> service.
    /// </summary>
    public IReadOnlyCollection<string>? LivenessCheckTypes { get; set; }

    /// <summary>
    /// When set (and <see cref="EnableHealthChecks"/> is on), the named grpc.health.v1 service
    /// <c>"readiness"</c> reports only the Benzene checks whose <c>Type</c> is in this list (the place
    /// for external-dependency checks). Null (the default) publishes no separate readiness service.
    /// </summary>
    public IReadOnlyCollection<string>? ReadinessCheckTypes { get; set; }

    /// <summary>
    /// When <c>true</c>, registers ASP.NET Core's gRPC server reflection service (grpc.reflection.v1alpha),
    /// letting tools like <c>grpcurl</c> discover services without a local .proto file. Map the service
    /// with <c>MapBenzeneGrpcReflectionService</c>.
    /// </summary>
    public bool EnableReflection { get; set; }

    /// <summary>Additional gRPC service configuration, applied after Benzene registers <see cref="BenzeneInterceptor"/>.</summary>
    public Action<GrpcServiceOptions>? ConfigureGrpc { get; set; }
}
