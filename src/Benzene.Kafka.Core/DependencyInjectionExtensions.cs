using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Kafka.Core.KafkaMessage;

namespace Benzene.Kafka.Core;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddKafka<TKey, TValue>(this IBenzeneServiceContainer services)
    {
        services.AddScoped<IMessageTopicGetter<KafkaRecordContext<TKey, TValue>>, KafkaMessageTopicGetter<TKey, TValue>>();
        services.AddHeaderMessageVersionGetter<KafkaRecordContext<TKey, TValue>>();
        services.AddScoped<IMessageHeadersGetter<KafkaRecordContext<TKey, TValue>>, KafkaMessageHeadersGetter<TKey, TValue>>();
        services.AddScoped<IMessageBodyGetter<KafkaRecordContext<TKey, TValue>>, KafkaMessageBodyGetter<TKey, TValue>>();
        services.AddScoped<IMessageHandlerResultSetter<KafkaRecordContext<TKey, TValue>>, KafkaMessageHandlerResultSetter<TKey, TValue>>();

        // Declare this transport for ITransportsInfo (the same name the per-invocation current-transport
        // records), matching the AWS/Azure Kafka adapters - it was previously omitted here.
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.Kafka));

        return services;
    }
}