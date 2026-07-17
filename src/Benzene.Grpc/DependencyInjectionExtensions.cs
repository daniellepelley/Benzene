using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Grpc.Serialization;

namespace Benzene.Grpc;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to route gRPC calls to message handlers.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container, for method chaining.</returns>
    /// <remarks>
    /// Called automatically by Benzene.Grpc.AspNet's <c>UseGrpc</c> extension; you don't normally need to
    /// call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddGrpcMessageHandlers(this IBenzeneServiceContainer services)
    {
        services.TryAddSingleton<IGrpcMethodFinder, ReflectionGrpcMethodFinder>();
        services.TryAddSingleton<IGrpcRouteFinder, GrpcRouteFinder>();
        services.AddScoped<IMessageTopicGetter<GrpcContext>, GrpcMessageTopicGetter>();
        services.AddScoped<IMessageVersionGetter<GrpcContext>, HeaderMessageVersionGetter<GrpcContext>>();
        services.AddScoped<IMessageBodyGetter<GrpcContext>, GrpcMessageBodyGetter>();
        services.AddScoped<IMessageHeadersGetter<GrpcContext>, GrpcMessageHeadersGetter>();
        services.AddScoped<IMessageHandlerResultSetter<GrpcContext>, GrpcMessageMessageHandlerResultSetter>();
        services.TryAddScoped<IGrpcMessageAdapter, ProtobufJsonGrpcMessageAdapter>();
        services.AddScoped<IRequestMapper<GrpcContext>, GrpcRequestMapper>();
        services.AddScoped<MessageRouter<GrpcContext>>();

        services.TryAddSingleton<IGrpcStatusCodeMapper, DefaultGrpcStatusCodeMapper>();
        services.AddScoped<GrpcServerCallAccessor>();
        services.AddScoped<IGrpcServerCallAccessor>(x => x.GetService<GrpcServerCallAccessor>());

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("grpc"));
        services.AddContextItems();
        return services;
    }
}
