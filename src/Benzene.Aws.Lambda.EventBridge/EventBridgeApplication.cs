using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;
using Microsoft.Extensions.Logging;

namespace Benzene.Aws.Lambda.EventBridge;

/// <summary>
/// Runs one EventBridge event through the <see cref="EventBridgeContext"/> middleware pipeline —
/// a single-context application (one pipeline invocation per event), since EventBridge invokes a
/// Lambda target with exactly one event, not a batch. Exception/failure-status behavior is
/// configurable via <see cref="EventBridgeOptions"/>, mirroring <c>SnsApplication</c>.
/// </summary>
public class EventBridgeApplication : IMiddlewareApplication<EventBridgeEvent>
{
    private readonly IMiddlewarePipeline<EventBridgeContext> _pipeline;
    private readonly EventBridgeOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBridgeApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built EventBridge middleware pipeline to run each event through.</param>
    /// <param name="options">
    /// Configures how a handler's exceptions and failure results are handled. Defaults to a new
    /// <see cref="EventBridgeOptions"/> instance (both flags off) if omitted.
    /// </param>
    public EventBridgeApplication(IMiddlewarePipeline<EventBridgeContext> pipeline, EventBridgeOptions options = null)
    {
        _pipeline = new TransportMiddlewarePipeline<EventBridgeContext>(TransportNames.EventBridge, pipeline);
        _options = options ?? new EventBridgeOptions();
    }

    /// <summary>
    /// Handles a single EventBridge event, running it through the pipeline in its own service scope.
    /// Whether a failure result propagates out of this call (and therefore fails the Lambda
    /// invocation, so the rule target's retry policy applies) is governed by
    /// <see cref="EventBridgeOptions.RaiseOnFailureStatus"/>/<see cref="EventBridgeOptions.CatchExceptions"/>.
    /// </summary>
    /// <param name="event">The EventBridge event to process.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to create the per-event scope.</param>
    public async Task HandleAsync(EventBridgeEvent @event, IServiceResolverFactory serviceResolverFactory)
    {
        var context = new EventBridgeContext(@event);

        try
        {
            using (var scope = serviceResolverFactory.CreateScope())
            {
                await _pipeline.HandleAsync(context, scope);
            }

            if (_options.RaiseOnFailureStatus && context.MessageResult?.IsSuccessful == false)
            {
                throw new EventBridgeMessageProcessingException(context.Event.Id);
            }
        }
        catch (Exception ex) when (_options.CatchExceptions)
        {
            using var loggingScope = serviceResolverFactory.CreateScope();
            loggingScope.GetService<ILogger<EventBridgeApplication>>()
                .LogError(ex, "Processing EventBridge event {id} failed", context.Event.Id);
        }
    }
}
