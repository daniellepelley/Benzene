using System.Reflection;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Kafka.Core.KafkaMessage;

namespace Benzene.Kafka.Core;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddKafkaMessageHandlers<Tkey, TValue>(this IBenzeneServiceContainer services, Assembly assembly)
    {
        services.AddScoped<IMessageTopicGetter<KafkaRecordContext<Tkey, TValue>>, KafkaMessageTopicGetter<Tkey, TValue>>();
        services.AddScoped<IMessageHeadersGetter<KafkaRecordContext<Tkey, TValue>>, KafkaMessageHeadersGetter<Tkey, TValue>>();
        services.AddScoped<IMessageBodyGetter<KafkaRecordContext<Tkey, TValue>>, KafkaMessageBodyGetter<Tkey, TValue>>();
            
        return services;
    }
}