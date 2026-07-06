using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Kafka.Core.Kafka;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddSendKafka(this IBenzeneServiceContainer services)
    {
        services.AddScoped<IMessageTopicGetter<KafkaSendMessageContext>, KafkaSendMessageTopicGetter>();
        services.AddScoped<IMessageHeadersGetter<KafkaSendMessageContext>, KafkaSendMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<KafkaSendMessageContext>, KafkaSendMessageBodyGetter>();

        return services;
    }
}