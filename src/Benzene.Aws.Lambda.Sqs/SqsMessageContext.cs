using Amazon.Lambda.SQSEvents;

namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// Provides the middleware pipeline context for a single record within an SQS batch event.
/// </summary>
public class SqsMessageContext
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
    /// <see cref="SqsMessageHandlerResultSetter"/>; null if no result has been set yet.
    /// </summary>
    public bool? IsSuccessful { get; set; }
}
