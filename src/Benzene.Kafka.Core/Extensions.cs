using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Kafka.Core.KafkaMessage;
using Benzene.SelfHost;
using Microsoft.Extensions.Logging;

namespace Benzene.Kafka.Core;

public static class Extensions
{
    /// <param name="app">The worker startup to add the Kafka consumer to.</param>
    /// <param name="benzeneKafkaConfig">The consumer configuration and processing behavior to use.</param>
    /// <param name="action">The action that configures the inner Kafka message pipeline.</param>
    /// <param name="consumerFactory">
    /// Optionally supplies the underlying <c>IConsumer</c> - for <c>ConsumerBuilder</c>
    /// configuration plain <c>ConsumerConfig</c> can't express (deserializers, handlers,
    /// <c>SetOAuthBearerTokenRefreshHandler</c> for secretless OAUTHBEARER auth). Defaults to
    /// building straight from the config, preserving the original behavior.
    /// </param>
    public static IBenzeneWorkerStartup UseKafka<TKey, TValue>(this IBenzeneWorkerStartup app, BenzeneKafkaConfig benzeneKafkaConfig, Action<IMiddlewarePipelineBuilder<KafkaRecordContext<TKey, TValue>>> action, IKafkaConsumerFactory<TKey, TValue>? consumerFactory = null)
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
            return new BenzeneKafkaWorker<TKey, TValue>(serviceResolverFactory, kafkaApplication, benzeneKafkaConfig, logger, consumerFactory);
        });
        return app;
    }
}
