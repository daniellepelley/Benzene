using Benzene.Abstractions.DI;
using Benzene.Clients;
using Benzene.Grpc.Serialization;

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
    public static IBenzeneServiceContainer AddGrpcClient(this IBenzeneServiceContainer services, Action<IGrpcClientRouteRegistry> configureRoutes)
    {
        var routeRegistry = new GrpcClientRouteRegistry();
        configureRoutes(routeRegistry);

        services.TryAddSingleton<IGrpcClientRouteRegistry>(routeRegistry);
        services.TryAddScoped<IGrpcMessageAdapter, ProtobufJsonGrpcMessageAdapter>();
        services.TryAddSingleton<IGrpcStatusReverseMapper, DefaultGrpcStatusReverseMapper>();
        services.AddScoped<IBenzeneMessageClient, GrpcBenzeneMessageClient>();

        return services;
    }
}
