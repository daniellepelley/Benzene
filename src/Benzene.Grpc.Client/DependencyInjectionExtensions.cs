using Benzene.Abstractions.DI;
using Benzene.Clients;
using Benzene.Grpc.Serialization;
using Benzene.HealthChecks.Core;
using Grpc.Net.Client;

namespace Benzene.Grpc.Client;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers an <see cref="IBenzeneMessageClient"/> backed by gRPC. <paramref name="configureRoutes"/>
    /// registers each outbound RPC's topic, protobuf wire types, and full method name (see
    /// <see cref="IGrpcClientRouteRegistry.Add{TRequest,TResponse}"/>). A <see cref="Grpc.Net.Client.GrpcChannel"/>
    /// must already be resolvable in the container - this method does not create or own one, matching how
    /// callers own their own <c>IProducer</c>/<c>HttpClient</c> for the other outbound clients.
    /// </summary>
    /// <param name="services">The service container to register on.</param>
    /// <param name="configureRoutes">Registers the outbound RPC routes.</param>
    /// <param name="healthCheck">
    /// When <c>true</c> (the default) a non-destructive gRPC transport-reachability check
    /// (<c>ConnectAsync</c> against the registered <see cref="Grpc.Net.Client.GrpcChannel"/>) is
    /// auto-registered on the deep <c>healthcheck</c> layer — never a Kubernetes probe (see
    /// <see cref="IDependencyHealthCheck"/>). Pass <c>false</c> to opt out.
    /// </param>
    public static IBenzeneServiceContainer AddGrpcClient(this IBenzeneServiceContainer services, Action<IGrpcClientRouteRegistry> configureRoutes, bool healthCheck = true)
    {
        var routeRegistry = new GrpcClientRouteRegistry();
        configureRoutes(routeRegistry);

        services.TryAddSingleton<IGrpcClientRouteRegistry>(routeRegistry);
        services.TryAddScoped<IGrpcMessageAdapter, ProtobufJsonGrpcMessageAdapter>();
        services.TryAddSingleton<IGrpcStatusReverseMapper, DefaultGrpcStatusReverseMapper>();
        services.AddScoped<IBenzeneMessageClient, GrpcBenzeneMessageClient>();

        if (healthCheck)
        {
            // Resolves the caller's registered GrpcChannel and probes transport reachability. Deep
            // healthcheck layer only, deduped by check type (one channel per container is the norm).
            services.AddDependencyHealthCheck(resolver => new GrpcHealthCheck(resolver.GetService<GrpcChannel>()));
        }

        return services;
    }

    /// <summary>
    /// Registers a <see cref="GrpcHealthCheck"/> on an explicit health-check builder, resolving the
    /// <see cref="Grpc.Net.Client.GrpcChannel"/> from the container.
    /// </summary>
    /// <param name="builder">The health check builder to register against.</param>
    /// <returns>The health check builder for method chaining.</returns>
    public static IHealthCheckBuilder AddGrpcHealthCheck(this IHealthCheckBuilder builder)
    {
        return builder.AddHealthCheck(resolver => new GrpcHealthCheck(resolver.GetService<GrpcChannel>()));
    }
}
