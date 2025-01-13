﻿using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Core.Info;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Aws.Kafka;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddKafka(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicGetter<KafkaContext>, KafkaMessageTopicGetter>();
        services.AddScoped<IMessageHeadersGetter<KafkaContext>, KafkaMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<KafkaContext>, KafkaMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<KafkaContext>, KafkaMessageMessageHandlerResultSetter>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("kafka"));
        return services;
    }
}