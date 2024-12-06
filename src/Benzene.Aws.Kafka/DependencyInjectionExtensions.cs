using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Mappers;
using Benzene.Core.Info;
using Benzene.Core.Serialization;

namespace Benzene.Aws.Kafka;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddKafka(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicMapper<KafkaContext>, KafkaMessageTopicMapper>();
        services.AddScoped<IMessageHeadersMapper<KafkaContext>, KafkaMessageHeadersMapper>();
        services.AddScoped<IMessageBodyMapper<KafkaContext>, KafkaMessageBodyMapper>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("kafka"));
        return services;
    }
}


