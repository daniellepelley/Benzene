using System;
using System.Threading.Tasks;
using Azure.Messaging;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.EventGrid;

/// <summary>
/// Converts between an outbound <see cref="OutboundContext"/> and an <see cref="EventGridSendMessageContext"/>
/// carrying a CloudEvents 1.0 <see cref="CloudEvent"/>, so an outbound route
/// (<c>OutboundRoutingBuilder.Route</c>) can send via Azure Event Grid. The <see cref="OutboundContext"/>
/// counterpart of <see cref="EventGridContextConverter{T}"/>.
/// </summary>
/// <remarks>
/// Event Grid has no request/response semantics beyond a send acknowledgement, so the response this
/// converter produces is always <see cref="IBenzeneResult{Void}"/> - a topic routed here must be sent
/// via <c>IBenzeneMessageSender.SendAsync&lt;TRequest,Void&gt;</c>.
/// </remarks>
public class OutboundEventGridContextConverter : IContextConverter<OutboundContext, EventGridSendMessageContext>
{
    private readonly string _source;
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundEventGridContextConverter"/> class using a
    /// <see cref="JsonSerializer"/> to serialize the outgoing message.
    /// </summary>
    /// <param name="source">The CloudEvent <c>source</c> - identifies the context the event happened in.</param>
    public OutboundEventGridContextConverter(string source)
        : this(source, new JsonSerializer())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundEventGridContextConverter"/> class.
    /// </summary>
    /// <param name="source">The CloudEvent <c>source</c> - identifies the context the event happened in.</param>
    /// <param name="serializer">The serializer used to serialize the outgoing message.</param>
    public OutboundEventGridContextConverter(string source, ISerializer serializer)
    {
        _source = source;
        _serializer = serializer;
    }

    /// <summary>
    /// Builds a CloudEvent, serializing the outgoing message as its data, setting the topic as the
    /// CloudEvent <c>type</c>, and forwarding headers as CloudEvent extension attributes.
    /// </summary>
    /// <param name="contextIn">The outbound context to convert.</param>
    /// <returns>A task that resolves to the built <see cref="EventGridSendMessageContext"/>.</returns>
    public Task<EventGridSendMessageContext> CreateRequestAsync(OutboundContext contextIn)
    {
        var cloudEvent = new CloudEvent(_source, contextIn.Topic, BinaryData.FromString(_serializer.Serialize(contextIn.Request)), "application/json", CloudEventDataFormat.Json);
        foreach (var header in contextIn.Headers)
        {
            cloudEvent.ExtensionAttributes[header.Key.ToLowerInvariant()] = header.Value;
        }

        return Task.FromResult(new EventGridSendMessageContext(cloudEvent));
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
