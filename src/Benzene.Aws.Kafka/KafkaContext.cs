using Amazon.Lambda.KafkaEvents;
using Benzene.Abstractions.Results;


namespace Benzene.Aws.Kafka;

public class KafkaContext : IHasMessageResult
{
    public KafkaContext(KafkaEvent kafkaEvent, KafkaEvent.KafkaEventRecord kafkaEventRecord)
    {
        KafkaEvent = kafkaEvent;
        KafkaEventRecord = kafkaEventRecord;
    }

    public KafkaEvent KafkaEvent { get; }
    public KafkaEvent.KafkaEventRecord KafkaEventRecord { get; }
    public IMessageResult MessageResult { get; set; }
}
