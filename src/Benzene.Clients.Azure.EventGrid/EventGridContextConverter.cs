using System;
using System.Threading.Tasks;
using Azure.Messaging;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.EventGrid;

/// <summary>
/// Converts between a generic Benzene client context and an <see cref="EventGridSendMessageContext"/>
/// carrying a CloudEvents 1.0 <see cref="CloudEvent"/>, so that a Benzene client pipeline can send
/// messages via Azure Event Grid. This is the schema Benzene's Event Grid ingress prefers - see
/// <see cref="EventGridEventSchemaContextConverter{T}"/> for the classic Event Grid schema instead.
/// </summary>
/// <typeparam name="T">The type of the outgoing message.</typeparam>
public class EventGridContextConverter<T> : IContextConverter<IBenzeneClientContext<T, Void>, EventGridSendMessageContext>
{
    private readonly string _source;
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventGridContextConverter{T}"/> class using a
    /// <see cref="JsonSerializer"/> to serialize the outgoing message.
    /// </summary>
    /// <param name="source">The CloudEvent <c>source</c> - identifies the context the event happened in.</param>
    public EventGridContextConverter(string source)
        : this(source, new JsonSerializer())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventGridContextConverter{T}"/> class.
    /// </summary>
    /// <param name="source">The CloudEvent <c>source</c> - identifies the context the event happened in.</param>
    /// <param name="serializer">The serializer used to serialize the outgoing message.</param>
    public EventGridContextConverter(string source, ISerializer serializer)
    {
        _source = source;
        _serializer = serializer;
    }

    /// <summary>
    /// Builds a CloudEvent, serializing the outgoing message as its data, setting the topic as the
    /// CloudEvent <c>type</c> (the property Benzene's Event Grid ingress routes on), and forwarding
    /// headers as CloudEvent extension attributes.
    /// </summary>
    /// <param name="contextIn">The incoming Benzene client context.</param>
    /// <returns>A task that resolves to the built <see cref="EventGridSendMessageContext"/>.</returns>
    /// <remarks>
    /// Benzene's Event Grid ingress does not currently read CloudEvent extension attributes back into
    /// message headers (unlike the application-property forwarding on Service Bus/Event Hubs) - see this
    /// package's <c>CLAUDE.md</c>. Extension attributes are still set here because they are the correct
    /// CloudEvents-spec mechanism for custom metadata and are visible to any CloudEvents-compliant
    /// subscriber, even though a same-stack Benzene handler won't see them as headers yet.
    /// </remarks>
    public Task<EventGridSendMessageContext> CreateRequestAsync(IBenzeneClientContext<T, Void> contextIn)
    {
        var cloudEvent = new CloudEvent(_source, contextIn.Request.Topic, BinaryData.FromString(_serializer.Serialize(contextIn.Request.Message)), "application/json", CloudEventDataFormat.Json);
        foreach (var header in contextIn.Request.Headers)
        {
            cloudEvent.ExtensionAttributes[header.Key.ToLowerInvariant()] = header.Value;
        }

        return Task.FromResult(new EventGridSendMessageContext(cloudEvent));
    }

    /// <summary>
    /// Marks the incoming Benzene client context as accepted. Event Grid has no request/response
    /// semantics beyond a send acknowledgement.
    /// </summary>
    /// <param name="contextIn">The incoming Benzene client context to set the response on.</param>
    /// <param name="contextOut">The completed <see cref="EventGridSendMessageContext"/>.</param>
    /// <returns>A completed task.</returns>
    public Task MapResponseAsync(IBenzeneClientContext<T, Void> contextIn, EventGridSendMessageContext contextOut)
    {
        contextIn.Response = BenzeneResult.Accepted<Void>();
        return Task.CompletedTask;
    }
}
