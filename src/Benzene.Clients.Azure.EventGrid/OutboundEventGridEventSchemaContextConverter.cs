using System;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.EventGrid;

/// <summary>
/// Converts between an outbound <see cref="OutboundContext"/> and an <see cref="EventGridSendMessageContext"/>
/// carrying a classic-schema <see cref="EventGridEvent"/>, so an outbound route
/// (<c>OutboundRoutingBuilder.Route</c>) can send via Azure Event Grid using the classic schema. The
/// <see cref="OutboundContext"/> counterpart of <see cref="EventGridEventSchemaContextConverter{T}"/>.
/// </summary>
/// <remarks>
/// Event Grid has no request/response semantics beyond a send acknowledgement, so the response this
/// converter produces is always <see cref="IBenzeneResult{Void}"/>. The classic schema has no header
/// bag - unlike <see cref="OutboundEventGridContextConverter"/> (CloudEvents), headers are not
/// forwarded here.
/// </remarks>
public class OutboundEventGridEventSchemaContextConverter : IContextConverter<OutboundContext, EventGridSendMessageContext>
{
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundEventGridEventSchemaContextConverter"/> class
    /// using a <see cref="JsonSerializer"/> to serialize the outgoing message.
    /// </summary>
    public OutboundEventGridEventSchemaContextConverter()
        : this(new JsonSerializer())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundEventGridEventSchemaContextConverter"/> class.
    /// </summary>
    /// <param name="serializer">The serializer used to serialize the outgoing message.</param>
    public OutboundEventGridEventSchemaContextConverter(ISerializer serializer)
    {
        _serializer = serializer;
    }

    /// <summary>
    /// Builds an Event Grid schema event, serializing the outgoing message as its data and setting the
    /// topic as both the event's <c>subject</c> and <c>eventType</c>.
    /// </summary>
    /// <param name="contextIn">The outbound context to convert.</param>
    /// <returns>A task that resolves to the built <see cref="EventGridSendMessageContext"/>.</returns>
    public Task<EventGridSendMessageContext> CreateRequestAsync(OutboundContext contextIn)
    {
        var eventGridEvent = new EventGridEvent(
            subject: contextIn.Topic,
            eventType: contextIn.Topic,
            dataVersion: "1.0",
            data: BinaryData.FromString(_serializer.Serialize(contextIn.Request)));

        return Task.FromResult(new EventGridSendMessageContext(eventGridEvent));
    }

    /// <summary>
    /// Marks the outbound context as accepted. Event Grid has no request/response semantics beyond a
    /// send acknowledgement.
    /// </summary>
    /// <param name="contextIn">The outbound context to set the response on.</param>
    /// <param name="contextOut">The completed <see cref="EventGridSendMessageContext"/>.</param>
    /// <returns>A completed task.</returns>
    public Task MapResponseAsync(OutboundContext contextIn, EventGridSendMessageContext contextOut)
    {
        contextIn.Response = BenzeneResult.Accepted<Void>();
        return Task.CompletedTask;
    }
}
