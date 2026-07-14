using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// The entry point application for a Service Bus-triggered Azure Function. Maps each message in the
/// triggered batch to a <see cref="ServiceBusContext"/> and runs them all through the middleware pipeline,
/// tagging the transport as <c>"service-bus"</c> for the duration.
/// </summary>
public class ServiceBusApplication : EntryPointMiddlewareApplication<ServiceBusReceivedMessage[]>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built Service Bus middleware pipeline to run each message through.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each batch.</param>
    public ServiceBusApplication(IMiddlewarePipeline<ServiceBusContext> pipeline, IServiceResolverFactory serviceResolverFactory)
        : base(new MiddlewareMultiApplication<ServiceBusReceivedMessage[], ServiceBusContext>(
                new TransportMiddlewarePipeline<ServiceBusContext>("service-bus", pipeline),
            messages => messages.Select(message => new ServiceBusContext(message)).ToArray()),
            serviceResolverFactory)
    { }
}
