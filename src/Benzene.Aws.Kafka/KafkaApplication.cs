using System.Linq;
using Amazon.Lambda.KafkaEvents;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Kafka;

public class KafkaApplication : MiddlewareMultiApplication<KafkaEvent, KafkaContext>
{
    public KafkaApplication(IMiddlewarePipeline<KafkaContext> pipeline)
        : base("kafka", pipeline,
            @event => @event.Records.Values.SelectMany(records => records.Select(record => new KafkaContext(@event, record))).ToArray()
        )
    { }
}
