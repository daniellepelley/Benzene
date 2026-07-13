using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Benzene.Grpc.AspNet;

/// <summary>
/// Maps the optional gRPC services enabled via <see cref="BenzeneGrpcOptions"/> (see <c>AddBenzeneGrpc</c>).
/// Thin wrappers over the standard <c>Grpc.AspNetCore.HealthChecks</c>/<c>Grpc.AspNetCore.Server.Reflection</c>
/// endpoint extensions, named to match Benzene's own conventions.
/// </summary>
public static class GrpcEndpointExtensions
{
    /// <summary>Maps grpc.health.v1's health check service. Requires <see cref="BenzeneGrpcOptions.EnableHealthChecks"/>.</summary>
    public static IEndpointConventionBuilder MapBenzeneGrpcHealthService(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGrpcHealthChecksService();
    }

    /// <summary>Maps grpc.reflection.v1alpha's server reflection service. Requires <see cref="BenzeneGrpcOptions.EnableReflection"/>.</summary>
    public static IEndpointConventionBuilder MapBenzeneGrpcReflectionService(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGrpcReflectionService();
    }
}
