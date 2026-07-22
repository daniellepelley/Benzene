using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.Function.EventHub.Function;

/// <summary>
/// Extracts the message body from an event — the raw serialized payload the sender wrote as the event
/// body (the property-based routing path's counterpart to <see cref="EventHubMessageTopicGetter"/>).
/// </summary>
public class EventHubMessageBodyGetter : IMessageBodyGetter<EventHubContext>
{
    /// <summary>
    /// Gets the event's body as a string.
    /// </summary>
    /// <param name="context">The Event Hub context to extract the body from.</param>
    /// <returns>The event body.</returns>
    public string GetBody(EventHubContext context)
    {
        return context.EventData.EventBody?.ToString();
    }
}
