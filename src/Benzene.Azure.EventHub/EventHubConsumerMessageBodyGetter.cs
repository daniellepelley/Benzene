using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.EventHub;

/// <summary>
/// Extracts the event body from an event received by the self-hosted consumer.
/// </summary>
public class EventHubConsumerMessageBodyGetter : IMessageBodyGetter<EventHubConsumerContext>
{
    /// <summary>
    /// Gets the event's body as a string.
    /// </summary>
    /// <param name="context">The Event Hub consumer context to extract the body from.</param>
    /// <returns>The event body.</returns>
    public string? GetBody(EventHubConsumerContext context)
    {
        return context.EventData.EventBody?.ToString();
    }
}
