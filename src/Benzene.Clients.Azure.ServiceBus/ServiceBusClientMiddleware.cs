using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.Middleware;

namespace Benzene.Clients.Azure.ServiceBus;

/// <summary>
/// Middleware that sends the <see cref="ServiceBusSendMessageContext"/>'s message via a
/// <see cref="ServiceBusSender"/> and records that the send completed.
/// </summary>
public class ServiceBusClientMiddleware : IMiddleware<ServiceBusSendMessageContext>
{
    private readonly ServiceBusSender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusClientMiddleware"/> class.
    /// </summary>
    /// <param name="sender">The Service Bus sender (bound to a queue or topic) used to send the message.</param>
    public ServiceBusClientMiddleware(ServiceBusSender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Gets the name of this middleware.
    /// </summary>
    public string Name => nameof(ServiceBusClientMiddleware);

    /// <summary>
    /// Sends the context's message via Service Bus. This is a terminal middleware; it does not call
    /// <paramref name="next"/>. Service Bus returns no payload, so success is a completed send.
    /// </summary>
    /// <param name="context">The context carrying the message to send.</param>
    /// <param name="next">Unused; this middleware does not delegate further down the pipeline.</param>
    public async Task HandleAsync(ServiceBusSendMessageContext context, Func<Task> next)
    {
        await _sender.SendMessageAsync(context.Message);
        context.IsSent = true;
    }
}
