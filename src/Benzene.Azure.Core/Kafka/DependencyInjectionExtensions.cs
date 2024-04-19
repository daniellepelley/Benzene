using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.DI;
using Benzene.Core.Info;
using Benzene.Core.Serialization;

namespace Benzene.Azure.Core.Kafka;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddAzureKafka(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicMapper<KafkaContext>, KafkaMessageTopicMapper>();
        services.AddScoped<IMessageHeadersMapper<KafkaContext>, KafkaMessageHeadersMapper>();
        services.AddScoped<IMessageBodyMapper<KafkaContext>, KafkaMessageBodyMapper>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("kafka"));
        return services;
    }
    
    public static IAzureAppBuilder UseKafka(this IAzureAppBuilder app, Action<IMiddlewarePipelineBuilder<KafkaContext>> action)
    {
        app.Register(x => x.AddAzureKafka());
        var pipeline = app.Create<KafkaContext>();
        action(pipeline);
        app.Add(serviceResolverFactory => new KafkaApplication(pipeline.Build(), serviceResolverFactory));
        return app;
    }
}


