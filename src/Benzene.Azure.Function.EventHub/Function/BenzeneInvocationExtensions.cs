using System;
using System.Collections.Generic;
using System.Globalization;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.Core;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.EventHub.Function;

/// <summary>
/// Provides the per-event <see cref="IBenzeneInvocation"/> for the Event Hub trigger's fan-out
/// pipeline.
/// </summary>
public static class BenzeneInvocationExtensions
{
    /// <summary>
    /// Adds middleware that exposes an <see cref="IBenzeneInvocation"/> for the duration of each
    /// event's dispatch, with <see cref="IBenzeneInvocation.InvocationId"/> set to the event's
    /// service-assigned <c>SequenceNumber</c>.
    /// </summary>
    /// <remarks>
    /// Each event in the trigger's batch is dispatched through its own DI scope
    /// (<c>EventHubApplication</c>'s <c>MiddlewareMultiApplication</c>-driven per-event
    /// <c>serviceResolverFactory.CreateScope()</c>), which doesn't inherit whatever
    /// <see cref="IBenzeneInvocation"/> the outer Azure Functions invocation populated (the
    /// Function's own <c>FunctionContext.InvocationId</c> - see
    /// <c>Benzene.Azure.Function.Core.FunctionsWorkerApplicationBuilderExtensions</c>). Auto-wired
    /// by <c>UseEventHub(...)</c> as the first middleware in the fan-out pipeline, so no application
    /// code changes are required.
    /// </remarks>
    /// <param name="app">The pipeline builder to add the invocation middleware to.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<EventHubContext> UseBenzeneInvocation(
        this IMiddlewarePipelineBuilder<EventHubContext> app)
    {
        return app.UseBenzeneInvocation((_, context) =>
            new BenzeneInvocation(
                context.EventData.SequenceNumber.ToString(CultureInfo.InvariantCulture),
                AzureFunctionAppBuilder.PlatformName,
                new Dictionary<Type, object>()));
    }
}
