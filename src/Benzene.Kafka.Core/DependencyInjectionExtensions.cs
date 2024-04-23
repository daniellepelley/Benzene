using System.Reflection;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Request;
using Benzene.Core.Request;
using Benzene.Kafka.Core.KafkaMessage;

namespace Benzene.Kafka.Core;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddKafkaMessageHandlers<Tkey, TValue>(this IBenzeneServiceContainer services, Assembly assembly)
    {
        services.AddScoped<IMessageTopicMapper<KafkaRecordContext<Tkey, TValue>>, KafkaMessageTopicMapper<Tkey, TValue>>();
        services.AddScoped<IMessageHeadersMapper<KafkaRecordContext<Tkey, TValue>>, KafkaMessageHeadersMapper<Tkey, TValue>>();
        services.AddScoped<IMessageBodyMapper<KafkaRecordContext<Tkey, TValue>>, KafkaMessageBodyMapper<Tkey, TValue>>();
            
        return services;
    }
}