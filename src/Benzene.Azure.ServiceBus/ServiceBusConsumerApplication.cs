using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// Processes a single received Service Bus message by mapping it to a
/// <see cref="ServiceBusConsumerContext"/> and running it through the middleware pipeline in its own
/// service scope, tagging the transport as <c>"service-bus"</c> for the duration. Returns the
/// handler's recorded <see cref="IMessageResult"/> (possibly <c>null</c> if nothing set one), which
/// <see cref="BenzeneServiceBusWorker"/> reads to support <see cref="ServiceBusConsumerAckMode.Explicit"/>.
/// </summary>
public class ServiceBusConsumerApplication : MiddlewareApplication<ServiceBusReceivedMessage, ServiceBusConsumerContext, IMessageResult?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusConsumerApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built Service Bus middleware pipeline to run each message through.</param>
    public ServiceBusConsumerApplication(IMiddlewarePipeline<ServiceBusConsumerContext> pipeline)
        : base(new TransportMiddlewarePipeline<ServiceBusConsumerContext>("service-bus", pipeline),
            ServiceBusConsumerContext.CreateInstance,
            context => context.MessageResult)
    { }
}
