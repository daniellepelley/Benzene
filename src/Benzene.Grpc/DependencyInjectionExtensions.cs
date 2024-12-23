using Benzene.Abstractions.DI;
using Benzene.Abstractions.Mappers;
using Benzene.Core.DI;

namespace Benzene.Grpc;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddGrpc(this IBenzeneServiceContainer services)
    {
        services.AddScoped<IGrpcRouteFinder, GrpcRouteFinder>();
        services.AddScoped<IGrpcMethodFinder, ReflectionGrpcMethodFinder>();
        services.AddScoped<IMessageTopicMapper<GrpcContext>, GrpcMessageTopicMapper>();
        services.AddScoped<IMessageBodyMapper<GrpcContext>, GrpcMessageBodyMapper>();
        services.AddScoped<IMessageHeadersMapper<GrpcContext>, GrpcMessageHeadersMapper>();
        services.AddScoped<IResultSetter<GrpcContext>, GrpcMessageResultSetter>();
        services.AddContextItems();
        return services;
    }
}