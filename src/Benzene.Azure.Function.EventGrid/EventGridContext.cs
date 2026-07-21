using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;

namespace Benzene.Azure.Function.EventGrid;

/// <summary>
/// Provides the middleware pipeline context for a single event within an Azure Functions Event Grid
/// trigger invocation.
/// </summary>
public class EventGridContext : IHasMessageResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventGridContext"/> class.
    /// </summary>
    /// <param name="event">The delivered event.</param>
    public EventGridContext(EventGridTriggerEvent @event)
    {
        Event = @event;
    }

    /// <summary>
    /// Gets the delivered event.
    /// </summary>
    public EventGridTriggerEvent Event { get; }

    /// <summary>
    /// Gets or sets the result of handling this event. Event Grid deliveries are fire-and-forget
    /// from the handler's perspective (a thrown exception is what triggers Event Grid's own
    /// retry/dead-letter machinery), so this is recorded for middleware/diagnostics only.
    /// </summary>
    public IBenzeneResult MessageResult { get; set; } = null!;
}
