using Azure.Messaging.ServiceBus;

namespace Benzene.Clients.Azure.ServiceBus;

/// <summary>
/// Provides the middleware pipeline context for sending a single message to Azure Service Bus.
/// </summary>
public class ServiceBusSendMessageContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusSendMessageContext"/> class.
    /// </summary>
    /// <param name="message">The Service Bus message to send.</param>
    public ServiceBusSendMessageContext(ServiceBusMessage message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets the Service Bus message to send.
    /// </summary>
    public ServiceBusMessage Message { get; }

    /// <summary>
    /// Gets or sets whether the message was sent. Set by <see cref="ServiceBusClientMiddleware"/> once
    /// the send completes without throwing. Service Bus <c>SendMessageAsync</c> returns no payload, so a
    /// completed send is an acknowledgement only.
    /// </summary>
    public bool IsSent { get; set; }
}
