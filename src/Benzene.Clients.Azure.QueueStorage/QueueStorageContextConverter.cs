using System.Threading.Tasks;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Results;

namespace Benzene.Clients.Azure.QueueStorage;

/// <summary>
/// Converts between a generic Benzene client context and a <see cref="QueueStorageSendMessageContext"/>,
/// so that a Benzene client pipeline can send messages via Azure Queue Storage. Serializes a
/// <see cref="BenzeneMessageRequest"/> envelope (topic, headers, body) as the queue message text - the
/// same envelope shape <c>BenzeneMessageQueueStorageHandler</c> (<c>queue.UseBenzeneMessage(...)</c>)
/// reads on the ingress side. A queue message has no properties/attributes bag on this transport, so
/// the envelope is the only way to carry a topic and headers.
/// </summary>
/// <typeparam name="T">The type of the outgoing message.</typeparam>
/// <remarks>
/// If the destination queue instead uses a fixed <c>UsePresetTopic(...)</c> route (no envelope; the
/// body <em>is</em> the request payload), do not use this converter - send the serialized payload
/// directly via a <see cref="Azure.Storage.Queues.QueueClient"/> instead. See this package's
/// <c>CLAUDE.md</c>.
/// </remarks>
public class QueueStorageContextConverter<T> : IContextConverter<IBenzeneClientContext<T, Void>, QueueStorageSendMessageContext>
{
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueStorageContextConverter{T}"/> class using a
    /// <see cref="JsonSerializer"/> to serialize the envelope and the outgoing message.
    /// </summary>
    public QueueStorageContextConverter()
        : this(new JsonSerializer())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueStorageContextConverter{T}"/> class.
    /// </summary>
    /// <param name="serializer">The serializer used to serialize the envelope and the outgoing message.</param>
    public QueueStorageContextConverter(ISerializer serializer)
    {
        _serializer = serializer;
    }

    /// <summary>
    /// Builds a queue send context, serializing a <see cref="BenzeneMessageRequest"/> envelope carrying
    /// the topic, headers, and the outgoing message as the body.
    /// </summary>
    /// <param name="contextIn">The incoming Benzene client context.</param>
    /// <returns>A task that resolves to the built <see cref="QueueStorageSendMessageContext"/>.</returns>
    public Task<QueueStorageSendMessageContext> CreateRequestAsync(IBenzeneClientContext<T, Void> contextIn)
    {
        var envelope = new BenzeneMessageRequest
        {
            Topic = contextIn.Request.Topic,
            Headers = contextIn.Request.Headers,
            Body = _serializer.Serialize(contextIn.Request.Message)
        };

        return Task.FromResult(new QueueStorageSendMessageContext(_serializer.Serialize(envelope)));
    }

    /// <summary>
    /// Marks the incoming Benzene client context as accepted. Queue Storage has no request/response
    /// semantics beyond a send acknowledgement.
    /// </summary>
    /// <param name="contextIn">The incoming Benzene client context to set the response on.</param>
    /// <param name="contextOut">The completed <see cref="QueueStorageSendMessageContext"/>.</param>
    /// <returns>A completed task.</returns>
    public Task MapResponseAsync(IBenzeneClientContext<T, Void> contextIn, QueueStorageSendMessageContext contextOut)
    {
        contextIn.Response = BenzeneResult.Accepted<Void>();
        return Task.CompletedTask;
    }
}
