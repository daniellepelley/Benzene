using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;

namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// Provides the middleware pipeline context for a single Service Bus message within an Azure Functions
/// Service Bus trigger invocation.
/// </summary>
public class ServiceBusContext : IHasMessageResult, IHasPresetTopic
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
    public IMessageResult MessageResult { get; set; }

    /// <summary>
    /// Gets or sets the preset topic for this context, set by <c>PresetTopicMiddleware</c> (via the
    /// <c>UsePresetTopic</c> pipeline extension) when this subscription routes every message to one
    /// fixed topic regardless of its <c>"topic"</c> application property. Null unless that's configured.
    /// </summary>
    public ITopic? PresetTopic { get; set; }
}
