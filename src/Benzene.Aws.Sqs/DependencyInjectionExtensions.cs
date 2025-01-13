using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Request;

namespace Benzene.Aws.Sqs;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddSqsConsumer(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<ISqsClientFactory, SqsClientFactory>();
        services.AddScoped<IMessageTopicGetter<SqsConsumerMessageContext>, SqsConsumerMessageTopicGetter>();
        services.AddScoped<IMessageHeadersGetter<SqsConsumerMessageContext>, SqsConsumerMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<SqsConsumerMessageContext>, SqsConsumerMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<SqsConsumerMessageContext>, SqsConsumerMessageMessageHandlerResultSetter>();
        services
            .AddScoped<IRequestMapper<SqsConsumerMessageContext>,
                MultiSerializerOptionsRequestMapper<SqsConsumerMessageContext, JsonSerializer>>();

        return services;
    }
}