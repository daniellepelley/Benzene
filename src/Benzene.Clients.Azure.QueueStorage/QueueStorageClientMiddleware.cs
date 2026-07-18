using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Benzene.Abstractions.Middleware;

namespace Benzene.Clients.Azure.QueueStorage;

/// <summary>
/// Middleware that sends the <see cref="QueueStorageSendMessageContext"/>'s text via a
/// <see cref="QueueClient"/> and records that the send completed.
/// </summary>
public class QueueStorageClientMiddleware : IMiddleware<QueueStorageSendMessageContext>
{
    private readonly QueueClient _queueClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueStorageClientMiddleware"/> class.
    /// </summary>
    /// <param name="queueClient">The queue client used to send the message.</param>
    public QueueStorageClientMiddleware(QueueClient queueClient)
    {
        _queueClient = queueClient;
    }

    /// <summary>
    /// Gets the name of this middleware.
    /// </summary>
    public string Name => nameof(QueueStorageClientMiddleware);

    /// <summary>
    /// Sends the context's message text to the queue. This is a terminal middleware; it does not call
    /// <paramref name="next"/>.
    /// </summary>
    /// <param name="context">The context carrying the message text to send.</param>
    /// <param name="next">Unused; this middleware does not delegate further down the pipeline.</param>
    public async Task HandleAsync(QueueStorageSendMessageContext context, Func<Task> next)
    {
        await _queueClient.SendMessageAsync(context.MessageText);
        context.IsSent = true;
    }
}
