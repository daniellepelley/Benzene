using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.Function.EventGrid;

/// <summary>
/// Surfaces an Event Grid event's envelope fields (id, subject, source) as message headers, so
/// middleware and handlers can reach them without the transport context.
/// </summary>
public class EventGridMessageHeadersGetter : IMessageHeadersGetter<EventGridContext>
{
    /// <summary>
    /// Gets the headers for the Event Grid event: <c>id</c>, <c>subject</c>, and <c>source</c>
    /// (whichever are present on the event).
    /// </summary>
    /// <param name="context">The Event Grid context to extract headers from.</param>
    /// <returns>The message headers.</returns>
    public IDictionary<string, string> GetHeaders(EventGridContext context)
    {
        var headers = new Dictionary<string, string>();

        if (context.Event.Id != null)
        {
            headers["id"] = context.Event.Id;
        }

        if (context.Event.Subject != null)
        {
            headers["subject"] = context.Event.Subject;
        }

        if (context.Event.Source != null)
        {
            headers["source"] = context.Event.Source;
        }

        return headers;
    }
}
