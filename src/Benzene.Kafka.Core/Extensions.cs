using Benzene.Abstractions.Middleware;
using Benzene.Core.DI;
using Benzene.Core.MessageHandlers;
using Benzene.Kafka.Core.KafkaMessage;
using Benzene.SelfHost;
using Confluent.Kafka;

namespace Benzene.Kafka.Core;

public static class Extensions
{
    public static IBenzeneWorkerBuilder UseKafka(this IBenzeneWorkerBuilder app, BenzeneKafkaConfig benzeneKafkaConfig, Action<IMiddlewarePipelineBuilder<KafkaRecordContext<Ignore, string>>> action)
    {
        app.Register(x => x.AddBenzeneMessage());
        var middlewarePipelineBuilder = app.Create<KafkaRecordContext<Ignore, string>>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        
        var kafkaApplication = new KafkaApplication<Ignore, string>(pipeline);
        app.Add(serviceResolverFactory => new BenzeneKafkaWorker(serviceResolverFactory, kafkaApplication, benzeneKafkaConfig));
        return app;
    }
}
