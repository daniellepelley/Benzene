using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.DI;
using Benzene.HostedService;
using Benzene.Kafka.Core.KafkaMessage;
using Confluent.Kafka;

namespace Benzene.Kafka.Core;

public static class Extensions
{
    public static IHostedServiceAppBuilder UseKafka(this IHostedServiceAppBuilder app, BenzeneKafkaConfig benzeneKafkaConfig, Action<IMiddlewarePipelineBuilder<KafkaRecordContext<Ignore, string>>> action)
    {
        app.Register(x => x.AddBenzeneMessage());
        var middlewarePipelineBuilder = app.Create<KafkaRecordContext<Ignore, string>>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        
        var kafkaApplication = new KafkaApplication<Ignore, string>(pipeline);
        app.Add(serviceResolverFactory => new BenzeneKafkaConsumer(serviceResolverFactory, kafkaApplication, benzeneKafkaConfig));
        return app;
    }
}
