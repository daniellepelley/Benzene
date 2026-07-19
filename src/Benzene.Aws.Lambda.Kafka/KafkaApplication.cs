using System.Linq;
using Amazon.Lambda.KafkaEvents;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Processes a Kafka event by flattening its per-topic-partition records into a single list of
/// <see cref="KafkaContext"/> instances and running them all through the middleware pipeline, tagging
/// the transport as <c>"kafka"</c> for the duration.
/// </summary>
public class KafkaApplication : MiddlewareMultiApplication<KafkaEvent, KafkaContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built Kafka middleware pipeline to run each record through.</param>
    public KafkaApplication(IMiddlewarePipeline<KafkaContext> pipeline)
        : base(new TransportMiddlewarePipeline<KafkaContext>(TransportNames.Kafka, pipeline),
            @event => @event.Records.Values.SelectMany(records => records.Select(record => new KafkaContext(@event, record))).ToArray()
        )
    { }
}


