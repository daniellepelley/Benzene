using Benzene.Abstractions.MessageHandlers;
using Microsoft.Azure.WebJobs.Extensions.Kafka;

namespace Benzene.Azure.Function.Kafka;

/// <summary>
/// Provides the middleware pipeline context for a single Kafka event within an Azure Functions Kafka
/// trigger batch.
/// </summary>
public class KafkaContext : IHasMessageResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaContext"/> class.
    /// </summary>
    /// <param name="kafkaEvent">The Kafka event data for this record.</param>
    public KafkaContext(KafkaEventData<string> kafkaEvent)
    {
        KafkaEvent = kafkaEvent;
    }

    /// <summary>
    /// Gets the Kafka event data for this record.
    /// </summary>
    public KafkaEventData<string> KafkaEvent { get; }

    /// <summary>
    /// Gets or sets the result of handling this record's message.
    /// </summary>
    public IMessageResult MessageResult { get; set; }
}
