using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Producer;
using Benzene.Abstractions.Middleware;

namespace Benzene.Clients.Azure.EventHub;

/// <summary>
/// Middleware that sends the <see cref="EventHubSendMessageContext"/>'s event via an
/// <see cref="EventHubProducerClient"/> and records that the send completed.
/// </summary>
public class EventHubClientMiddleware : IMiddleware<EventHubSendMessageContext>
{
    private readonly EventHubProducerClient _producerClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubClientMiddleware"/> class.
    /// </summary>
    /// <param name="producerClient">The Event Hubs producer client used to send the event.</param>
    public EventHubClientMiddleware(EventHubProducerClient producerClient)
    {
        _producerClient = producerClient;
    }

    /// <summary>
    /// Gets the name of this middleware.
    /// </summary>
    public string Name => nameof(EventHubClientMiddleware);

    /// <summary>
    /// Sends the context's event via Event Hubs, as a single-event batch. This is a terminal middleware;
    /// it does not call <paramref name="next"/>. The producer client returns no payload, so success is a
    /// completed send.
    /// </summary>
    /// <param name="context">The context carrying the event to send.</param>
    /// <param name="next">Unused; this middleware does not delegate further down the pipeline.</param>
    public async Task HandleAsync(EventHubSendMessageContext context, Func<Task> next)
    {
        using var batch = await _producerClient.CreateBatchAsync();
        if (!batch.TryAdd(context.EventData))
        {
            throw new InvalidOperationException("The event is too large to fit in a single Event Hubs batch.");
        }

        await _producerClient.SendAsync(batch);
        context.IsSent = true;
    }
}
