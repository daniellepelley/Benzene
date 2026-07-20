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
    /// <param name="partitionKey">
    /// The partition key that co-locates related events on the same partition (preserving their
    /// order), or <c>null</c> to let Event Hubs distribute the event across partitions.
    /// </param>
    public EventHubSendMessageContext(EventData eventData, string partitionKey = null)
    {
        EventData = eventData;
        PartitionKey = partitionKey;
    }

    /// <summary>
    /// Gets the event to send.
    /// </summary>
    public EventData EventData { get; }

    /// <summary>
    /// Gets the partition key. When set, all events sharing a key land on the same partition and are
    /// delivered in order there - the only mechanism Event Hubs offers for per-key ordering. <c>null</c>
    /// lets the service distribute events across partitions (no ordering guarantee).
    /// </summary>
    public string PartitionKey { get; }

    /// <summary>
    /// Gets or sets whether the event was sent. Set by <see cref="EventHubClientMiddleware"/> once the
    /// send completes without throwing. The producer client returns no payload, so a completed send is
    /// an acknowledgement only.
    /// </summary>
    public bool IsSent { get; set; }
}
