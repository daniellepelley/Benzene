using Benzene.Abstractions.Results;

namespace Benzene.Clients;

/// <summary>
/// The single interface business logic depends on to send an outbound message: a topic and a
/// request, nothing else - no service name, no client type, no factory resolution at the call site.
/// Registered by <see cref="OutboundRoutingBuilder"/>/<c>AddOutboundRouting(...)</c>, which builds
/// one outbound pipeline per topic ahead of time. See
/// <c>work/benzene-clients-redesign-plan.md</c> §2.1 for the full design.
/// </summary>
public interface IBenzeneMessageSender
{
    /// <summary>
    /// Sends <paramref name="request"/> on <paramref name="topic"/> through that topic's registered
    /// outbound pipeline.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The expected response payload type.</typeparam>
    /// <param name="topic">The topic to send on - must have a route registered via
    /// <see cref="OutboundRoutingBuilder.Route"/>.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="headers">Per-call headers (e.g. a caller-supplied correlation/tenant value) -
    /// distinct from any headers a route's own middleware adds statically at
    /// <see cref="OutboundRoutingBuilder.Route"/>-configuration time. Optional; defaults to none.</param>
    /// <returns>The result of the send.</returns>
    /// <exception cref="UnroutedTopicException">No route is registered for <paramref name="topic"/>.</exception>
    Task<IBenzeneResult<TResponse>> SendAsync<TRequest, TResponse>(string topic, TRequest request, IDictionary<string, string>? headers = null);
}
