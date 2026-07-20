using System;

namespace Benzene.Azure.Function.QueueStorage;

/// <summary>
/// Thrown by <see cref="QueueStorageApplication"/> when
/// <see cref="QueueStorageOptions.RaiseOnFailureStatus"/> is enabled and a message handler reported an
/// unsuccessful result without itself throwing - escalating the failure into an exception so the
/// Functions host's <c>maxDequeueCount</c> retry/poison handling applies the same way it would for an
/// unhandled exception.
/// </summary>
public class QueueStorageMessageProcessingException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="QueueStorageMessageProcessingException"/> class.</summary>
    /// <param name="messageId">The Queue Storage message id the handler reported a failure for.</param>
    public QueueStorageMessageProcessingException(string messageId)
        : base($"Message handler reported an unsuccessful result for Queue Storage message {messageId}.")
    {
        MessageId = messageId;
    }

    /// <summary>Gets the Queue Storage message id the handler reported a failure for.</summary>
    public string MessageId { get; }
}
