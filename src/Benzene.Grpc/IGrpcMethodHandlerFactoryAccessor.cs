namespace Benzene.Grpc;

/// <summary>
/// Provides access to the <see cref="IGrpcMethodHandlerFactory"/> configured by <c>UseGrpc</c>.
/// </summary>
/// <remarks>
/// gRPC interceptors are activated per call by ASP.NET Core's own request-scoped dependency
/// injection, so <see cref="BenzeneInterceptor"/> cannot depend on a factory that is only known once
/// the middleware pipeline is built at <c>Configure</c> time (after the service provider is already
/// built). Hosting packages register a single accessor instance during <c>ConfigureServices</c> and
/// populate <see cref="Factory"/> once the pipeline is built, so the same instance is visible both to
/// ASP.NET Core's DI and to the code that builds the pipeline.
/// </remarks>
public interface IGrpcMethodHandlerFactoryAccessor
{
    /// <summary>The configured factory, or <c>null</c> if <c>UseGrpc</c> has not run yet.</summary>
    IGrpcMethodHandlerFactory? Factory { get; set; }
}
