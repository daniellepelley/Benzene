using System;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
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
/// carrying a classic-schema <see cref="EventGridEvent"/>, for publishers that still target the Event
/// Grid schema rather than CloudEvents 1.0. Prefer <see cref="EventGridContextConverter{T}"/> (CloudEvents)
/// for new code - it is the schema Benzene's Event Grid ingress prefers.
/// </summary>
/// <typeparam name="T">The type of the outgoing message.</typeparam>
/// <remarks>
/// The classic Event Grid schema has no free-form header bag, so unlike Service Bus/Event Hubs/CloudEvents,
/// headers (correlation id, W3C trace context) are <b>not</b> forwarded by this converter - there is
/// nowhere on an <see cref="EventGridEvent"/> to put them.
/// </remarks>
public class EventGridEventSchemaContextConverter<T> : IContextConverter<IBenzeneClientContext<T, Void>, EventGridSendMessageContext>
{
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventGridEventSchemaContextConverter{T}"/> class
    /// using a <see cref="JsonSerializer"/> to serialize the outgoing message.
    /// </summary>
    public EventGridEventSchemaContextConverter()
        : this(new JsonSerializer())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventGridEventSchemaContextConverter{T}"/> class.
    /// </summary>
    /// <param name="serializer">The serializer used to serialize the outgoing message.</param>
    public EventGridEventSchemaContextConverter(ISerializer serializer)
    {
        _serializer = serializer;
    }

    /// <summary>
    /// Builds an Event Grid schema event, serializing the outgoing message as its data and setting the
    /// topic as both the event's <c>subject</c> and <c>eventType</c> (the property Benzene's Event Grid
    /// ingress routes on).
    /// </summary>
    /// <param name="contextIn">The incoming Benzene client context.</param>
    /// <returns>A task that resolves to the built <see cref="EventGridSendMessageContext"/>.</returns>
    public Task<EventGridSendMessageContext> CreateRequestAsync(IBenzeneClientContext<T, Void> contextIn)
    {
        var eventGridEvent = new EventGridEvent(
            subject: contextIn.Request.Topic,
            eventType: contextIn.Request.Topic,
            dataVersion: "1.0",
            data: BinaryData.FromString(_serializer.Serialize(contextIn.Request.Message)));

        return Task.FromResult(new EventGridSendMessageContext(eventGridEvent));
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
