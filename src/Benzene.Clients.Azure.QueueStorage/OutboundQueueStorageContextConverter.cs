using System.Threading.Tasks;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.QueueStorage;

/// <summary>
/// Converts between an outbound <see cref="OutboundContext"/> and a <see cref="QueueStorageSendMessageContext"/>,
/// so an outbound route (<c>OutboundRoutingBuilder.Route</c>) can send via Azure Queue Storage. The
/// <see cref="OutboundContext"/> counterpart of <see cref="QueueStorageContextConverter{T}"/>.
/// </summary>
/// <remarks>
/// Queue Storage has no request/response semantics beyond a send acknowledgement, so the response this
/// converter produces is always <see cref="IBenzeneResult{Void}"/> - a topic routed here must be sent
/// via <c>IBenzeneMessageSender.SendAsync&lt;TRequest,Void&gt;</c>.
/// </remarks>
public class OutboundQueueStorageContextConverter : IContextConverter<OutboundContext, QueueStorageSendMessageContext>
{
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundQueueStorageContextConverter"/> class using
    /// a <see cref="JsonSerializer"/> to serialize the envelope and the outgoing message.
    /// </summary>
    public OutboundQueueStorageContextConverter()
        : this(new JsonSerializer())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundQueueStorageContextConverter"/> class.
    /// </summary>
    /// <param name="serializer">The serializer used to serialize the envelope and the outgoing message.</param>
    public OutboundQueueStorageContextConverter(ISerializer serializer)
    {
        _serializer = serializer;
    }

    /// <summary>
    /// Builds a queue send context, serializing a <see cref="BenzeneMessageRequest"/> envelope carrying
    /// the topic, headers, and the outgoing message as the body.
    /// </summary>
    /// <param name="contextIn">The outbound context to convert.</param>
    /// <returns>A task that resolves to the built <see cref="QueueStorageSendMessageContext"/>.</returns>
    public Task<QueueStorageSendMessageContext> CreateRequestAsync(OutboundContext contextIn)
    {
        var envelope = new BenzeneMessageRequest
        {
            Topic = contextIn.Topic,
            Headers = contextIn.Headers,
            Body = _serializer.Serialize(contextIn.Request)
        };

        return Task.FromResult(new QueueStorageSendMessageContext(_serializer.Serialize(envelope)));
    }

    /// <summary>
    /// Marks the outbound context as accepted. Queue Storage has no request/response semantics beyond a
    /// send acknowledgement.
    /// </summary>
    /// <param name="contextIn">The outbound context to set the response on.</param>
    /// <param name="contextOut">The completed <see cref="QueueStorageSendMessageContext"/>.</param>
    /// <returns>A completed task.</returns>
    public Task MapResponseAsync(OutboundContext contextIn, QueueStorageSendMessageContext contextOut)
    {
        contextIn.Response = BenzeneResult.Accepted<Void>();
        return Task.CompletedTask;
    }
}
