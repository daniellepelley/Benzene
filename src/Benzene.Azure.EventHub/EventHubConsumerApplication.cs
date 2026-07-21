using Azure.Messaging.EventHubs;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Azure.EventHub;

/// <summary>
/// Processes a single received event by mapping it to an <see cref="EventHubConsumerContext"/> and
/// running it through the middleware pipeline in its own service scope, tagging the transport as
/// <c>"event-hub"</c> for the duration. Returns the handler's recorded <see cref="IBenzeneResult"/>
/// (possibly <c>null</c> if nothing set one).
/// </summary>
public class EventHubConsumerApplication : MiddlewareApplication<EventData, EventHubConsumerContext, IBenzeneResult?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubConsumerApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built Event Hub middleware pipeline to run each event through.</param>
    public EventHubConsumerApplication(IMiddlewarePipeline<EventHubConsumerContext> pipeline)
        : base(new TransportMiddlewarePipeline<EventHubConsumerContext>(TransportNames.EventHub, pipeline),
            EventHubConsumerContext.CreateInstance,
            context => context.MessageResult)
    { }
}
