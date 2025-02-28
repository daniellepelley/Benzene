using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Kafka.Core.KafkaMessage;

namespace Benzene.Kafka.Core;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddKafka<TKey, TValue>(this IBenzeneServiceContainer services)
    {
        services.AddScoped<IMessageTopicGetter<KafkaRecordContext<TKey, TValue>>, KafkaMessageTopicGetter<TKey, TValue>>();
        services.AddScoped<IMessageHeadersGetter<KafkaRecordContext<TKey, TValue>>, KafkaMessageHeadersGetter<TKey, TValue>>();
        services.AddScoped<IMessageBodyGetter<KafkaRecordContext<TKey, TValue>>, KafkaMessageBodyGetter<TKey, TValue>>();
        services.AddScoped<IMessageHandlerResultSetter<KafkaRecordContext<TKey, TValue>>, KafkaMessageHandlerResultSetter<TKey, TValue>>();

        return services;
    }
}