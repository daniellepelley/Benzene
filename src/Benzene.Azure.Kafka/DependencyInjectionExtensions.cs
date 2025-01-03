﻿using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Core;
using Benzene.Core.Info;
using Benzene.Core.Serialization;

namespace Benzene.Azure.Kafka;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddAzureKafka(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicMapper<KafkaContext>, KafkaMessageTopicMapper>();
        services.AddScoped<IMessageHeadersMapper<KafkaContext>, KafkaMessageHeadersMapper>();
        services.AddScoped<IMessageBodyMapper<KafkaContext>, KafkaMessageBodyMapper>();
        services.AddScoped<IResultSetter<KafkaContext>, KafkaMessageResultSetter>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("kafka"));
        return services;
    }
    
    public static IAzureFunctionAppBuilder UseKafka(this IAzureFunctionAppBuilder app, Action<IMiddlewarePipelineBuilder<KafkaContext>> action)
    {
        app.Register(x => x.AddAzureKafka());
        var pipeline = app.Create<KafkaContext>();
        action(pipeline);
        app.Add(serviceResolverFactory => new KafkaApplication(pipeline.Build(), serviceResolverFactory));
        return app;
    }
}