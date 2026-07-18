using Azure.Messaging.EventHubs;

namespace Benzene.Clients.Azure.EventHub;

/// <summary>
/// Provides the middleware pipeline context for sending a single event to Azure Event Hubs.
/// </summary>
public class EventHubSendMessageContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubSendMessageContext"/> class.
    /// </summary>
    /// <param name="eventData">The event to send.</param>
    public EventHubSendMessageContext(EventData eventData)
    {
        EventData = eventData;
    }

    /// <summary>
    /// Gets the event to send.
    /// </summary>
    public EventData EventData { get; }

    /// <summary>
    /// Gets or sets whether the event was sent. Set by <see cref="EventHubClientMiddleware"/> once the
    /// send completes without throwing. The producer client returns no payload, so a completed send is
    /// an acknowledgement only.
    /// </summary>
    public bool IsSent { get; set; }
}
