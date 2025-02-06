using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;
using Microsoft.Azure.WebJobs.Extensions.Kafka;

namespace Benzene.Azure.Kafka;

public class KafkaApplication : EntryPointMiddlewareApplication<KafkaEventData<string>[]>
{
    public KafkaApplication(IMiddlewarePipeline<KafkaContext> pipeline, IServiceResolverFactory serviceResolverFactory)
        : base(new MiddlewareMultiApplication<KafkaEventData<string>[], KafkaContext>(
                new TransportMiddlewarePipeline<KafkaContext>("kafka", pipeline),
            kafkaEvents => kafkaEvents.Select(kafkaEvent => new KafkaContext(kafkaEvent)).ToArray()),
            serviceResolverFactory)
    { }
}
