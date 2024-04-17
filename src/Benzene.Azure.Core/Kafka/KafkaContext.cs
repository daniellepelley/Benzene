using Benzene.Abstractions.Results;
using Microsoft.Azure.WebJobs.Extensions.Kafka;

namespace Benzene.Azure.Core.Kafka;

public class KafkaContext : IHasMessageResult
{
    public KafkaContext(KafkaEventData<string> kafkaEvent)
    {
        KafkaEvent = kafkaEvent;
        MessageResult = Benzene.Core.Results.MessageResult.Empty();
    }

    public KafkaEventData<string> KafkaEvent { get; }

    public IMessageResult MessageResult { get; set; }
}
