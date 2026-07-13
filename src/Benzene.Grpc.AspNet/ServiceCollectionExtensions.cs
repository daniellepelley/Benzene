using Benzene.Grpc;
using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Benzene.Grpc.AspNet;

/// <summary>
/// Provides extension methods for registering gRPC services and the Benzene interceptor in ASP.NET Core.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ASP.NET Core's gRPC services and adds <see cref="BenzeneInterceptor"/> so that gRPC calls
    /// matching a <see cref="GrpcMethodAttribute"/>-decorated message handler are routed through Benzene's
    /// message handler pipeline.
    /// </summary>
    /// <param name="services">The service collection to register gRPC services with.</param>
    /// <param name="configure">Optional additional gRPC service configuration.</param>
    /// <returns><paramref name="services"/>, for method chaining.</returns>
    /// <remarks>
    /// Registers a single shared <see cref="IGrpcMethodHandlerFactoryAccessor"/> instance. <c>UseGrpc</c>
    /// populates its <see cref="IGrpcMethodHandlerFactoryAccessor.Factory"/> once the middleware pipeline
    /// is built; because the registration is instance-based it resolves to the same object from both the
    /// application's root service provider (which activates <see cref="BenzeneInterceptor"/> per call) and
    /// Benzene's own pipeline-building container.
    /// </remarks>
    public static IServiceCollection AddBenzeneGrpc(this IServiceCollection services, Action<GrpcServiceOptions>? configure = null)
    {
        services.TryAdd(ServiceDescriptor.Singleton<IGrpcMethodHandlerFactoryAccessor>(new GrpcMethodHandlerFactoryAccessor()));

        services.AddGrpc(options =>
        {
            options.Interceptors.Add(typeof(BenzeneInterceptor));
            configure?.Invoke(options);
        });

        return services;
    }
}
