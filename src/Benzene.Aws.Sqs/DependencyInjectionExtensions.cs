using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Serialization;

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