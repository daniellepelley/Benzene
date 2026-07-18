using Azure.Messaging;
using Azure.Messaging.EventGrid;

namespace Benzene.Clients.Azure.EventGrid;

/// <summary>
/// Provides the middleware pipeline context for sending a single event to Azure Event Grid, in either
/// the CloudEvents 1.0 schema or the classic Event Grid schema. Exactly one of <see cref="CloudEvent"/>
/// / <see cref="EventGridEvent"/> is set, matching which constructor was used.
/// </summary>
public class EventGridSendMessageContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventGridSendMessageContext"/> class for a
    /// CloudEvents 1.0 event - the schema Benzene's Event Grid ingress prefers.
    /// </summary>
    /// <param name="cloudEvent">The CloudEvent to send.</param>
    public EventGridSendMessageContext(CloudEvent cloudEvent)
    {
        CloudEvent = cloudEvent;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventGridSendMessageContext"/> class for a classic
    /// Event Grid schema event.
    /// </summary>
    /// <param name="eventGridEvent">The Event Grid schema event to send.</param>
    public EventGridSendMessageContext(EventGridEvent eventGridEvent)
    {
        EventGridEvent = eventGridEvent;
    }

    /// <summary>
    /// Gets the CloudEvent to send, or <see langword="null"/> if this context carries a classic
    /// <see cref="EventGridEvent"/> instead.
    /// </summary>
    public CloudEvent? CloudEvent { get; }

    /// <summary>
    /// Gets the classic Event Grid schema event to send, or <see langword="null"/> if this context
    /// carries a <see cref="CloudEvent"/> instead.
    /// </summary>
    public EventGridEvent? EventGridEvent { get; }

    /// <summary>
    /// Gets or sets whether the event was sent. Set by <see cref="EventGridClientMiddleware"/> once the
    /// send completes without throwing. The publisher client returns no payload, so a completed send is
    /// an acknowledgement only.
    /// </summary>
    public bool IsSent { get; set; }
}
