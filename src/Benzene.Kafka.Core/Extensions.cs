using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Kafka.Core.KafkaMessage;
using Benzene.SelfHost;

namespace Benzene.Kafka.Core;

public static class Extensions
{
    public static IBenzeneWorkerBuilder UseKafka<TKey, TValue>(this IBenzeneWorkerBuilder app, BenzeneKafkaConfig benzeneKafkaConfig, Action<IMiddlewarePipelineBuilder<KafkaRecordContext<TKey, TValue>>> action)
    {
        app.Register(x => x
            .AddBenzeneMessage()
            .AddKafka<TKey, TValue>()
        );
        var middlewarePipelineBuilder = app.Create<KafkaRecordContext<TKey, TValue>>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        
        var kafkaApplication = new KafkaApplication<TKey, TValue>(pipeline);
        app.Add(serviceResolverFactory => new BenzeneKafkaWorker<TKey, TValue>(serviceResolverFactory, kafkaApplication, benzeneKafkaConfig));
        return app;
    }
}
