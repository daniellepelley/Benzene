using System;

namespace Benzene.Aws.Lambda.Sns;

/// <summary>
/// Thrown by <see cref="SnsApplication"/> when <see cref="SnsOptions.RaiseOnFailureStatus"/> is
/// enabled and a message handler reported an unsuccessful result without itself throwing -
/// escalating the failure into an exception so SNS's own subscription retry policy applies the same
/// way it would for an unhandled exception.
/// </summary>
public class SnsMessageProcessingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnsMessageProcessingException"/> class.
    /// </summary>
    /// <param name="messageId">The SNS message ID that the handler reported a failure for.</param>
    public SnsMessageProcessingException(string messageId)
        : base($"Message handler reported an unsuccessful result for SNS message {messageId}.")
    {
        MessageId = messageId;
    }

    /// <summary>
    /// Gets the SNS message ID that the handler reported a failure for.
    /// </summary>
    public string MessageId { get; }
}
