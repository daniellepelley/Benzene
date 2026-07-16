using System;

namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// Thrown by <see cref="ServiceBusApplication"/> when <see cref="ServiceBusOptions.RaiseOnFailureStatus"/>
/// is enabled and a message handler reported an unsuccessful result without itself throwing -
/// escalating the failure into an exception so it's treated the same as an unhandled exception for
/// retry purposes.
/// </summary>
public class ServiceBusMessageProcessingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusMessageProcessingException"/> class.
    /// </summary>
    /// <param name="messageId">The Service Bus message ID that the handler reported a failure for.</param>
    public ServiceBusMessageProcessingException(string messageId)
        : base($"Message handler reported an unsuccessful result for Service Bus message {messageId}.")
    {
        MessageId = messageId;
    }

    /// <summary>
    /// Gets the Service Bus message ID that the handler reported a failure for.
    /// </summary>
    public string MessageId { get; }
}
