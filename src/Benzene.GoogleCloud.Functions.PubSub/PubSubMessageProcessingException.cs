namespace Benzene.GoogleCloud.Functions.PubSub;

/// <summary>
/// Thrown by <see cref="PubSubMiddlewareApplication"/> when
/// <see cref="PubSubOptions.RaiseOnFailureStatus"/> is enabled and a message handler reported an
/// unsuccessful result without itself throwing - escalating the failure into an exception so it's
/// treated the same as an unhandled exception for retry purposes.
/// </summary>
public class PubSubMessageProcessingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PubSubMessageProcessingException"/> class.
    /// </summary>
    /// <param name="messageId">The Pub/Sub message ID of the failing message.</param>
    public PubSubMessageProcessingException(string messageId)
        : base($"Message handler reported an unsuccessful result for Pub/Sub message {messageId}.")
    {
        MessageId = messageId;
    }

    /// <summary>
    /// Gets the Pub/Sub message ID of the failing message.
    /// </summary>
    public string MessageId { get; }
}
