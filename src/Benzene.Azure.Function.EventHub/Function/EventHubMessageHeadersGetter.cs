using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.Function.EventHub.Function;

/// <summary>
/// Extracts headers from an event's string-typed properties.
/// </summary>
public class EventHubMessageHeadersGetter : IMessageHeadersGetter<EventHubContext>
{
    /// <summary>
    /// Gets the headers for the event from its string-typed properties.
    /// </summary>
    /// <param name="context">The Event Hub context to extract headers from.</param>
    /// <returns>The message headers.</returns>
    public IDictionary<string, string> GetHeaders(EventHubContext context)
    {
        return context.EventData.Properties
            .Where(x => x.Value is string)
            .ToDictionary(x => x.Key, x => (string)x.Value);
    }
}
