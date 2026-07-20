using Benzene.Abstractions.Results;
using Benzene.Clients;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.ResponseEvents;

/// <summary>
/// The default <see cref="IResponseEventPublisher"/>: publishes through
/// <see cref="IBenzeneMessageSender"/>, so every event topic must have a route registered via
/// <c>AddOutboundRouting(...)</c> and the route's own middleware (correlation id, W3C trace
/// context, retry, ...) applies to the published event. Resolved from the handled message's DI
/// scope, so scoped state like the inbound correlation id flows onto the event's headers.
/// </summary>
/// <remarks>
/// Event routes are expected to be fire-and-forget transports whose pipelines set an
/// <see cref="IBenzeneResult{T}"/> of <see cref="Void"/> acknowledgement (SQS, SNS, EventBridge,
/// Kafka, ...). A route that produces a differently-typed response makes the send throw
/// <c>OutboundResponseTypeMismatchException</c>, which surfaces as a publish failure - register a
/// custom <see cref="IResponseEventPublisher"/> for such routes.
/// </remarks>
public class BenzeneMessageSenderResponseEventPublisher : IResponseEventPublisher
{
    private readonly IBenzeneMessageSender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneMessageSenderResponseEventPublisher"/> class.
    /// </summary>
    /// <param name="sender">The outbound sender to publish through.</param>
    public BenzeneMessageSenderResponseEventPublisher(IBenzeneMessageSender sender)
    {
        _sender = sender;
    }

    /// <inheritdoc />
    public async Task<IBenzeneResult> PublishAsync(string eventTopic, object payload, IDictionary<string, string>? headers = null)
    {
        return await _sender.SendAsync<object, Void>(eventTopic, payload, headers);
    }
}
