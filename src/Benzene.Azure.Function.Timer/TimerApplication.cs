using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.Timer;

/// <summary>
/// The entry point application for a timer-triggered Azure Function. Maps the tick to a
/// <see cref="TimerContext"/> and runs it through the middleware pipeline, tagging the transport as
/// <c>"timer"</c> for the duration.
/// </summary>
public class TimerApplication : EntryPointMiddlewareApplication<TimerTriggerInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimerApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built timer middleware pipeline to run each tick through.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each invocation.</param>
    public TimerApplication(IMiddlewarePipeline<TimerContext> pipeline, IServiceResolverFactory serviceResolverFactory)
        : base(new MiddlewareApplication<TimerTriggerInfo, TimerContext>(
                new TransportMiddlewarePipeline<TimerContext>(TransportNames.Timer, pipeline),
                timer => new TimerContext(timer)),
            serviceResolverFactory)
    { }
}
