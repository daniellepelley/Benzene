using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Core.Info;
using Benzene.Core.Request;
using Benzene.Core.Serialization;

namespace Benzene.Aws.Lambda.Sqs;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddSqs(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicMapper<SqsMessageContext>, SqsMessageTopicMapper>();
        services.AddScoped<IMessageHeadersMapper<SqsMessageContext>, SqsMessageHeadersMapper>();
        services.AddScoped<IMessageBodyMapper<SqsMessageContext>, SqsMessageBodyMapper>();
        services.AddScoped<IResultSetter<SqsMessageContext>, SqsMessageResultSetter>();
        services
            .AddScoped<IRequestMapper<SqsMessageContext>,
                MultiSerializerOptionsRequestMapper<SqsMessageContext, JsonSerializer>>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("sqs"));
        return services;
    }
}