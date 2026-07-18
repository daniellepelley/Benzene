using System;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Benzene.Abstractions.Middleware;

namespace Benzene.Clients.Azure.EventGrid;

/// <summary>
/// Middleware that sends the <see cref="EventGridSendMessageContext"/>'s event via an
/// <see cref="EventGridPublisherClient"/> and records that the send completed.
/// </summary>
public class EventGridClientMiddleware : IMiddleware<EventGridSendMessageContext>
{
    private readonly EventGridPublisherClient _publisherClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventGridClientMiddleware"/> class.
    /// </summary>
    /// <param name="publisherClient">The Event Grid publisher client used to send the event.</param>
    public EventGridClientMiddleware(EventGridPublisherClient publisherClient)
    {
        _publisherClient = publisherClient;
    }

    /// <summary>
    /// Gets the name of this middleware.
    /// </summary>
    public string Name => nameof(EventGridClientMiddleware);

    /// <summary>
    /// Sends the context's event via Event Grid, using whichever of <see cref="EventGridSendMessageContext.CloudEvent"/>
    /// / <see cref="EventGridSendMessageContext.EventGridEvent"/> is set. This is a terminal middleware; it
    /// does not call <paramref name="next"/>. The publisher client returns no payload, so success is a
    /// completed send.
    /// </summary>
    /// <param name="context">The context carrying the event to send.</param>
    /// <param name="next">Unused; this middleware does not delegate further down the pipeline.</param>
    public async Task HandleAsync(EventGridSendMessageContext context, Func<Task> next)
    {
        if (context.CloudEvent != null)
        {
            await _publisherClient.SendEventAsync(context.CloudEvent);
        }
        else
        {
            await _publisherClient.SendEventAsync(context.EventGridEvent!);
        }

        context.IsSent = true;
    }
}
