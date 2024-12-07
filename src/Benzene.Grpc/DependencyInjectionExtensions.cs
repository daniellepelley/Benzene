using Benzene.Abstractions.DI;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Core.DI;
using Benzene.Core.Mappers;
using Benzene.Core.MessageHandlers;

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
        services.AddContextItems();
        return services;
    }
}

public class GrpcMessageTopicMapper : IMessageTopicMapper<GrpcContext>
{
    public ITopic GetTopic(GrpcContext context)
    {
        return new Topic(context.Topic);
    }
}

public class GrpcMessageBodyMapper : IMessageBodyMapper<GrpcContext>
{
    public string? GetBody(GrpcContext context)
    {
        return System.Text.Json.JsonSerializer.Serialize(context.RequestAsObject);
    }
}
public class GrpcMessageHeadersMapper : IMessageHeadersMapper<GrpcContext>
{
    public IDictionary<string, string> GetHeaders(GrpcContext context)
    {
        return new Dictionary<string, string>();
    }
}

