using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Core.MessageHandlers;

namespace Benzene.Grpc;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddGrpc(this IBenzeneServiceContainer services)
    {
        services.AddScoped<IGrpcRouteFinder, GrpcRouteFinder>();
        services.AddScoped<IGrpcMethodFinder, ReflectionGrpcMethodFinder>();
        services.AddScoped<IMessageTopicGetter<GrpcContext>, GrpcMessageTopicGetter>();
        services.AddScoped<IMessageBodyGetter<GrpcContext>, GrpcMessageBodyGetter>();
        services.AddScoped<IMessageHeadersGetter<GrpcContext>, GrpcMessageHeadersGetter>();
        services.AddScoped<IMessageHandlerResultSetter<GrpcContext>, GrpcMessageMessageHandlerResultSetter>();
        services.AddContextItems();
        return services;
    }
}