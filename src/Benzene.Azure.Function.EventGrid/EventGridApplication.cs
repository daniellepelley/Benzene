using System;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;
using Microsoft.Extensions.Logging;

namespace Benzene.Azure.Function.EventGrid;

/// <summary>
/// The entry point application for an Event Grid-triggered Azure Function. Maps each event to an
/// <see cref="EventGridContext"/> and runs it through the middleware pipeline, tagging the transport
/// as <c>"event-grid"</c> for the duration. Exception/failure-status behavior is configurable via
/// <see cref="EventGridOptions"/>, mirroring <c>Benzene.Azure.Function.Kafka</c>.
/// </summary>
public class EventGridApplication : EntryPointMiddlewareApplication<EventGridTriggerEvent[]>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventGridApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built Event Grid middleware pipeline to run each event through.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each invocation.</param>
    /// <param name="options">
    /// Configures how a handler's exceptions and failure results are handled, and the batch fan-out
    /// concurrency. Defaults to a new <see cref="EventGridOptions"/> instance (both flags off) if omitted.
    /// </param>
    public EventGridApplication(IMiddlewarePipeline<EventGridContext> pipeline, IServiceResolverFactory serviceResolverFactory, EventGridOptions options = null)
        : base(new EventGridBatchApplication(pipeline, options), serviceResolverFactory)
    { }
}

/// <summary>
/// Runs every event in an Event Grid delivery through the middleware pipeline concurrently, each in
/// its own service scope, applying <see cref="EventGridOptions"/> to decide whether an event's
/// exception or failure result is contained (logged) or left to cascade and fail the invocation (so
/// Event Grid's retry/dead-letter policy engages).
/// </summary>
public class EventGridBatchApplication : IMiddlewareApplication<EventGridTriggerEvent[]>
{
    private readonly IMiddlewarePipeline<EventGridContext> _pipeline;
    private readonly EventGridOptions _options;

    public EventGridBatchApplication(IMiddlewarePipeline<EventGridContext> pipeline, EventGridOptions options = null)
    {
        _pipeline = new TransportMiddlewarePipeline<EventGridContext>(TransportNames.EventGrid, pipeline);
        _options = options ?? new EventGridOptions();
    }

    public async Task HandleAsync(EventGridTriggerEvent[] @event, IServiceResolverFactory serviceResolverFactory)
    {
        var contexts = @event.Select(gridEvent => new EventGridContext(gridEvent));
        await BoundedFanOut.WhenAllAsync(contexts, async context =>
            {
                try
                {
                    using (var scope = serviceResolverFactory.CreateScope())
                    {
                        await _pipeline.HandleAsync(context, scope);
                    }

                    if (_options.RaiseOnFailureStatus && context.MessageResult?.IsSuccessful == false)
                    {
                        throw new EventGridMessageProcessingException(context.Event.Id ?? context.Event.EventType ?? "unknown");
                    }
                }
                catch (Exception ex) when (_options.CatchExceptions)
                {
                    using (var loggingScope = serviceResolverFactory.CreateScope())
                    {
                        loggingScope.GetService<ILogger<EventGridApplication>>()
                            .LogError(ex, "Processing Event Grid event {id} failed", context.Event.Id);
                    }
                }
            }, _options.MaxDegreeOfParallelism);
    }
}
