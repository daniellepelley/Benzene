using Azure.Messaging.EventHubs;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.EventHub.Function;

/// <summary>
/// The entry point application for an Event Hub-triggered Azure Function. Maps each event in the
/// triggered batch to an <see cref="EventHubContext"/> and runs them all through the middleware
/// pipeline, tagging the transport as <c>"event-hub"</c> for the duration.
/// </summary>
public class EventHubApplication : EntryPointMiddlewareApplication<EventData[]>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubApplication"/> class.
    /// </summary>
    /// <param name="pipelineBuilder">The built Event Hub middleware pipeline to run each event through.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each batch.</param>
    public EventHubApplication(IMiddlewarePipeline<EventHubContext> pipelineBuilder, IServiceResolverFactory serviceResolverFactory)
        : base(new MiddlewareMultiApplication<EventData[], EventHubContext>(
                new TransportMiddlewarePipeline<EventHubContext>("event-hub", pipelineBuilder),
        @event => @event.Select(EventHubContext.CreateInstance).ToArray()),
            serviceResolverFactory)
    { }
}
