using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;
using Microsoft.Azure.WebJobs.Extensions.Kafka;

namespace Benzene.Azure.Function.Kafka;

/// <summary>
/// The entry point application for a Kafka-triggered Azure Function. Maps each event in the triggered
/// batch to a <see cref="KafkaContext"/> and runs them all through the middleware pipeline, tagging the
/// transport as <c>"kafka"</c> for the duration.
/// </summary>
public class KafkaApplication : EntryPointMiddlewareApplication<KafkaEventData<string>[]>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built Kafka middleware pipeline to run each event through.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each batch.</param>
    public KafkaApplication(IMiddlewarePipeline<KafkaContext> pipeline, IServiceResolverFactory serviceResolverFactory)
        : base(new MiddlewareMultiApplication<KafkaEventData<string>[], KafkaContext>(
                new TransportMiddlewarePipeline<KafkaContext>("kafka", pipeline),
            kafkaEvents => kafkaEvents.Select(kafkaEvent => new KafkaContext(kafkaEvent)).ToArray()),
            serviceResolverFactory)
    { }
}
