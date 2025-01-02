using Benzene.Abstractions.DI;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Core.Request;
using Benzene.Core.Serialization;

namespace Benzene.Aws.Sqs;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddSqsConsumer(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<ISqsClientFactory, SqsClientFactory>();
        services.AddScoped<IMessageTopicMapper<SqsConsumerMessageContext>, SqsConsumerMessageTopicMapper>();
        services.AddScoped<IMessageHeadersMapper<SqsConsumerMessageContext>, SqsConsumerMessageHeadersMapper>();
        services.AddScoped<IMessageBodyMapper<SqsConsumerMessageContext>, SqsConsumerMessageBodyMapper>();
        services.AddScoped<IResultSetter<SqsConsumerMessageContext>, SqsConsumerMessageResultSetter>();
        services
            .AddScoped<IRequestMapper<SqsConsumerMessageContext>,
                MultiSerializerOptionsRequestMapper<SqsConsumerMessageContext, JsonSerializer>>();

        return services;
    }
}