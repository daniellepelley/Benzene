using Amazon.Lambda.SQSEvents;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;

namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// Provides the middleware pipeline context for a single record within an SQS batch event.
/// </summary>
public class SqsMessageContext : IHasPresetTopic
{
    private SqsMessageContext(SQSEvent sqsEvent, SQSEvent.SQSMessage sqsMessage)
    {
        SqsMessage = sqsMessage;
        SqsEvent = sqsEvent;
    }

    /// <summary>
    /// Creates a new <see cref="SqsMessageContext"/> for a single record within an SQS batch event.
    /// </summary>
    /// <param name="sqsEvent">The full SQS batch event.</param>
    /// <param name="sqsMessage">The specific record within the batch this context represents.</param>
    /// <returns>The created context.</returns>
    public static SqsMessageContext CreateInstance(SQSEvent sqsEvent, SQSEvent.SQSMessage sqsMessage)
    {
        return new SqsMessageContext(sqsEvent, sqsMessage);
    }

    /// <summary>
    /// Gets the full SQS batch event this record belongs to.
    /// </summary>
    public SQSEvent SqsEvent { get; }

    /// <summary>
    /// Gets the specific SQS record this context represents.
    /// </summary>
    public SQSEvent.SQSMessage SqsMessage { get; }

    /// <summary>
    /// Gets or sets whether this record was handled successfully. Set by
    /// <see cref="SqsMessageMessageHandlerResultSetter"/>; null if no result has been set yet.
    /// </summary>
    public bool? IsSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the preset topic for this context, set by <c>PresetTopicMiddleware</c> (via the
    /// <c>UsePresetTopic</c> pipeline extension) when this queue routes every message to one fixed
    /// topic regardless of its <c>topic</c> message attribute. Null unless that's configured.
    /// </summary>
    public ITopic? PresetTopic { get; set; }
}
