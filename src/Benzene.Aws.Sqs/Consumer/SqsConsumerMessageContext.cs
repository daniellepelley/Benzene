using Amazon.SQS.Model;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;

namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Provides the middleware pipeline context for a single SQS message received by the polling consumer.
/// </summary>
public class SqsConsumerMessageContext : IHasPresetTopic
{
    private SqsConsumerMessageContext(Message message)
    {
        Message = message;
    }

    /// <summary>
    /// Creates a new <see cref="SqsConsumerMessageContext"/> for a received SQS message.
    /// </summary>
    /// <param name="message">The received SQS message.</param>
    /// <returns>The created context.</returns>
    public static SqsConsumerMessageContext CreateInstance(Message message)
    {
        return new SqsConsumerMessageContext(message);
    }

    /// <summary>
    /// Gets the received SQS message.
    /// </summary>
    public Message Message { get; }

    /// <summary>
    /// Gets or sets the preset topic for this context, set by <c>PresetTopicMiddleware</c> (via the
    /// <c>UsePresetTopic</c> pipeline extension) when this queue routes every message to one fixed
    /// topic regardless of its <c>topic</c> message attribute. Null unless that's configured.
    /// </summary>
    public ITopic? PresetTopic { get; set; }
}
