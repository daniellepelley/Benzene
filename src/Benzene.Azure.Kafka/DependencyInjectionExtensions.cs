using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Core;
using Benzene.Core.Info;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Azure.Kafka;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddAzureKafka(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicGetter<KafkaContext>, KafkaMessageTopicGetter>();
        services.AddScoped<IMessageHeadersGetter<KafkaContext>, KafkaMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<KafkaContext>, KafkaMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<KafkaContext>, KafkaMessageMessageHandlerResultSetter>();

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