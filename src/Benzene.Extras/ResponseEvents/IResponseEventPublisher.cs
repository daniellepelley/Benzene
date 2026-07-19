using Benzene.Abstractions.Results;

namespace Benzene.Extras.ResponseEvents;

/// <summary>
/// The outbound port <see cref="ResponseEventsMiddleware{TRequest,TResponse}"/> publishes matched
/// events through. The default implementation
/// (<see cref="BenzeneMessageSenderResponseEventPublisher"/>) sends via
/// <c>IBenzeneMessageSender</c>'s outbound routing; replace the scoped registration to publish
/// differently (a test fake, a custom fan-out, or - later - a transactional outbox relay).
/// </summary>
public interface IResponseEventPublisher
{
    /// <summary>
    /// Publishes one response event.
    /// </summary>
    /// <param name="eventTopic">The topic id to publish on.</param>
    /// <param name="payload">The event payload.</param>
    /// <param name="headers">Optional per-event headers, merged with whatever the outbound route's own middleware adds.</param>
    /// <returns>The outcome of the publish; an unsuccessful result is treated as a publish failure.</returns>
    Task<IBenzeneResult> PublishAsync(string eventTopic, object payload, IDictionary<string, string>? headers = null);
}
