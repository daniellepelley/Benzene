using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// Provides the middleware pipeline context for a single Service Bus message received by the
/// self-hosted consumer (<see cref="BenzeneServiceBusWorker"/>).
/// </summary>
public class ServiceBusConsumerContext : IHasMessageResult
{
    private ServiceBusConsumerContext(ServiceBusReceivedMessage message)
    {
        Message = message;
    }

    /// <summary>
    /// Creates a new <see cref="ServiceBusConsumerContext"/> for a received Service Bus message.
    /// </summary>
    /// <param name="message">The received Service Bus message.</param>
    /// <returns>The created context.</returns>
    public static ServiceBusConsumerContext CreateInstance(ServiceBusReceivedMessage message)
    {
        return new ServiceBusConsumerContext(message);
    }

    /// <summary>
    /// Gets the received Service Bus message.
    /// </summary>
    public ServiceBusReceivedMessage Message { get; }

    /// <summary>
    /// Gets or sets the result of handling this message. Set by
    /// <see cref="ServiceBusConsumerMessageHandlerResultSetter"/>; read by
    /// <see cref="BenzeneServiceBusWorker"/> to support <see cref="ServiceBusConsumerAckMode.Explicit"/>.
    /// </summary>
    public IBenzeneResult MessageResult { get; set; } = null!;
}
