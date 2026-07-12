using Amazon.Lambda.KafkaEvents;
using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Provides the middleware pipeline context for a single record within a Kafka event.
/// </summary>
public class KafkaContext : IHasMessageResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaContext"/> class.
    /// </summary>
    /// <param name="kafkaEvent">The full Kafka event, spanning one or more topic partitions.</param>
    /// <param name="kafkaEventRecord">The specific record within the event this context represents.</param>
    public KafkaContext(KafkaEvent kafkaEvent, KafkaEvent.KafkaEventRecord kafkaEventRecord)
    {
        KafkaEvent = kafkaEvent;
        KafkaEventRecord = kafkaEventRecord;
    }

    /// <summary>
    /// Gets the full Kafka event this record belongs to.
    /// </summary>
    public KafkaEvent KafkaEvent { get; }

    /// <summary>
    /// Gets the specific Kafka record this context represents.
    /// </summary>
    public KafkaEvent.KafkaEventRecord KafkaEventRecord { get; }

    /// <summary>
    /// Gets or sets the result of handling this record. Set by <see cref="KafkaMessageMessageHandlerResultSetter"/>.
    /// </summary>
    public IMessageResult MessageResult { get; set; }
}
