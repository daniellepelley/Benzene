using Azure.Messaging.EventHubs;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Core.EventHub;

public class EventHubApplication : EntryPointMiddlewareApplication<EventData[]>
{
    public EventHubApplication(IMiddlewarePipeline<EventHubContext> pipelineBuilder, IServiceResolverFactory serviceResolverFactory)
        : base(new MiddlewareMultiApplication<EventData[], EventHubContext>("event-hub", pipelineBuilder,
        @event => @event.Select(EventHubContext.CreateInstance).ToArray()),
            serviceResolverFactory)
    { }
}
