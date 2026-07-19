using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.EventGrid;

/// <summary>
/// The entry point application for an Event Grid-triggered Azure Function. Maps each event to an
/// <see cref="EventGridContext"/> and runs it through the middleware pipeline, tagging the transport
/// as <c>"event-grid"</c> for the duration. The trigger delivers one event per invocation by
/// default; the array event shape covers batched delivery (trigger cardinality "many") and tests.
/// </summary>
public class EventGridApplication : EntryPointMiddlewareApplication<EventGridTriggerEvent[]>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventGridApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built Event Grid middleware pipeline to run each event through.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each invocation.</param>
    public EventGridApplication(IMiddlewarePipeline<EventGridContext> pipeline, IServiceResolverFactory serviceResolverFactory)
        : base(new MiddlewareMultiApplication<EventGridTriggerEvent[], EventGridContext>(
                new TransportMiddlewarePipeline<EventGridContext>(TransportNames.EventGrid, pipeline),
                events => events.Select(@event => new EventGridContext(@event)).ToArray()),
            serviceResolverFactory)
    { }
}
