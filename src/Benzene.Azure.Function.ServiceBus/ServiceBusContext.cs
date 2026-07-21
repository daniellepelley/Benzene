using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;

namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// Provides the middleware pipeline context for a single Service Bus message within an Azure Functions
/// Service Bus trigger invocation.
/// </summary>
public class ServiceBusContext : IHasMessageResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusContext"/> class.
    /// </summary>
    /// <param name="message">The Service Bus message.</param>
    public ServiceBusContext(ServiceBusReceivedMessage message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets the Service Bus message.
    /// </summary>
    public ServiceBusReceivedMessage Message { get; }

    /// <summary>
    /// Gets or sets the result of handling this message.
    /// </summary>
    public IBenzeneResult MessageResult { get; set; }
}
