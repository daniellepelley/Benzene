using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.EventHub;

/// <summary>
/// Extracts headers from an event's string-typed properties.
/// </summary>
public class EventHubConsumerMessageHeadersGetter : IMessageHeadersGetter<EventHubConsumerContext>
{
    /// <summary>
    /// Gets the headers for the event from its string-typed properties.
    /// </summary>
    /// <param name="context">The Event Hub consumer context to extract headers from.</param>
    /// <returns>The message headers.</returns>
    public IDictionary<string, string> GetHeaders(EventHubConsumerContext context)
    {
        return context.EventData.Properties
            .Where(x => x.Value is string)
            .ToDictionary(x => x.Key, x => (string)x.Value);
    }
}
