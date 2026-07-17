using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.Function.EventGrid;

/// <summary>
/// Extracts the message body from an Event Grid event - its <c>data</c> payload as raw JSON, which
/// the request mapper then deserializes into the handler's request type.
/// </summary>
public class EventGridMessageBodyGetter : IMessageBodyGetter<EventGridContext>
{
    /// <summary>
    /// Gets the event's <c>data</c> payload as raw JSON, or <c>{}</c> when the event carries no
    /// data (so handlers with empty request types still bind).
    /// </summary>
    /// <param name="context">The Event Grid context to extract the body from.</param>
    /// <returns>The message body.</returns>
    public string GetBody(EventGridContext context)
    {
        return context.Event.Data?.GetRawText() ?? "{}";
    }
}
