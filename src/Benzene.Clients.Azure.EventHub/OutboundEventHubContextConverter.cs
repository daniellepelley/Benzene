using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.EventHub;

/// <summary>
/// Converts between an outbound <see cref="OutboundContext"/> and an
/// <see cref="EventHubSendMessageContext"/>, so an outbound route
/// (<c>OutboundRoutingBuilder.Route</c>) can send via Azure Event Hubs. The
/// <see cref="OutboundContext"/> counterpart of <see cref="EventHubContextConverter{T}"/>.
/// </summary>
/// <remarks>
/// Event Hubs has no request/response semantics beyond a send acknowledgement, so the response this
/// converter produces is always <see cref="IBenzeneResult{Void}"/> - a topic routed here must be sent
/// via <c>IBenzeneMessageSender.SendAsync&lt;TRequest,Void&gt;</c>.
/// </remarks>
public class OutboundEventHubContextConverter : IContextConverter<OutboundContext, EventHubSendMessageContext>
{
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundEventHubContextConverter"/> class using a
    /// <see cref="JsonSerializer"/> to serialize the outgoing message.
    /// </summary>
    public OutboundEventHubContextConverter()
        : this(new JsonSerializer())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundEventHubContextConverter"/> class.
    /// </summary>
    /// <param name="serializer">The serializer used to serialize the outgoing message.</param>
    public OutboundEventHubContextConverter(ISerializer serializer)
    {
        _serializer = serializer;
    }

    /// <summary>
    /// Builds an Event Hubs event, serializing the outgoing message as the body and setting the topic
    /// and headers as event properties.
    /// </summary>
    /// <param name="contextIn">The outbound context to convert.</param>
    /// <returns>A task that resolves to the built <see cref="EventHubSendMessageContext"/>.</returns>
    public Task<EventHubSendMessageContext> CreateRequestAsync(OutboundContext contextIn)
    {
        var eventData = new EventData(_serializer.Serialize(contextIn.Request));
        foreach (var header in contextIn.Headers)
        {
            eventData.Properties[header.Key] = header.Value;
        }

        eventData.Properties["topic"] = contextIn.Topic;

        return Task.FromResult(new EventHubSendMessageContext(eventData));
    }

    /// <summary>
    /// Marks the outbound context as accepted. Event Hubs has no request/response semantics beyond a
    /// send acknowledgement.
    /// </summary>
    /// <param name="contextIn">The outbound context to set the response on.</param>
    /// <param name="contextOut">The completed <see cref="EventHubSendMessageContext"/>.</param>
    /// <returns>A completed task.</returns>
    public Task MapResponseAsync(OutboundContext contextIn, EventHubSendMessageContext contextOut)
    {
        contextIn.Response = BenzeneResult.Accepted<Void>();
        return Task.CompletedTask;
    }
}
