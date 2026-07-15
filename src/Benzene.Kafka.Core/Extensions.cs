using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Kafka.Core.KafkaMessage;
using Benzene.SelfHost;
using Microsoft.Extensions.Logging;

namespace Benzene.Kafka.Core;

public static class Extensions
{
    public static IBenzeneWorkerStartup UseKafka<TKey, TValue>(this IBenzeneWorkerStartup app, BenzeneKafkaConfig benzeneKafkaConfig, Action<IMiddlewarePipelineBuilder<KafkaRecordContext<TKey, TValue>>> action)
    {
        app.Register(x => x
            .AddBenzeneMessage()
            .AddKafka<TKey, TValue>()
        );
        var middlewarePipelineBuilder = app.Create<KafkaRecordContext<TKey, TValue>>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();

        var kafkaApplication = new KafkaApplication<TKey, TValue>(pipeline);
        app.Add(serviceResolverFactory =>
        {
            using var scope = serviceResolverFactory.CreateScope();
            var logger = scope.GetService<ILogger<BenzeneKafkaWorker<TKey, TValue>>>();
            return new BenzeneKafkaWorker<TKey, TValue>(serviceResolverFactory, kafkaApplication, benzeneKafkaConfig, logger);
        });
        return app;
    }
}
