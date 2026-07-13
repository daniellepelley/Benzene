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
    /// When <c>true</c>, registers ASP.NET Core's gRPC server reflection service (grpc.reflection.v1alpha),
    /// letting tools like <c>grpcurl</c> discover services without a local .proto file. Map the service
    /// with <c>MapBenzeneGrpcReflectionService</c>.
    /// </summary>
    public bool EnableReflection { get; set; }

    /// <summary>Additional gRPC service configuration, applied after Benzene registers <see cref="BenzeneInterceptor"/>.</summary>
    public Action<GrpcServiceOptions>? ConfigureGrpc { get; set; }
}
