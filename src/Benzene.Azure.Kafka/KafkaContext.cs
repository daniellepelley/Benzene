using Benzene.Abstractions.MessageHandlers.ToDelete;
using Benzene.Abstractions.Results;
using Microsoft.Azure.WebJobs.Extensions.Kafka;

namespace Benzene.Azure.Kafka;

public class KafkaContext : IHasMessageResult
{
    public KafkaContext(KafkaEventData<string> kafkaEvent)
    {
        KafkaEvent = kafkaEvent;
    }

    public KafkaEventData<string> KafkaEvent { get; }

    public IMessageResult MessageResult { get; set; }
}
