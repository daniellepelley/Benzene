namespace Benzene.ResponseEvents;

/// <summary>
/// The outcome of a matched <see cref="IResponseEventMapping"/>: the event topic to publish on and
/// the payload to publish. Produced by <see cref="IResponseEventMapping.Resolve"/> and consumed by
/// <see cref="ResponseEventsMiddleware{TRequest,TResponse}"/>, which hands it to the registered
/// <see cref="IResponseEventPublisher"/>.
/// </summary>
public sealed class ResponseEventPublication
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseEventPublication"/> class.
    /// </summary>
    /// <param name="eventTopic">The topic id the event should be published on.</param>
    /// <param name="payload">The event payload (never null - a mapping with no payload resolves to no publication instead).</param>
    public ResponseEventPublication(string eventTopic, object payload)
    {
        EventTopic = eventTopic;
        Payload = payload;
    }

    /// <summary>The topic id the event should be published on.</summary>
    public string EventTopic { get; }

    /// <summary>The event payload.</summary>
    public object Payload { get; }
}
