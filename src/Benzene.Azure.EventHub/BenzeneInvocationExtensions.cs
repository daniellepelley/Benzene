using System;
using System.Collections.Generic;
using System.Globalization;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.SelfHost;

namespace Benzene.Azure.EventHub;

/// <summary>
/// Provides the per-event <see cref="IBenzeneInvocation"/> for the self-hosted Event Hub consumer
/// worker's pipeline.
/// </summary>
public static class BenzeneInvocationExtensions
{
    /// <summary>
    /// Adds middleware that exposes an <see cref="IBenzeneInvocation"/> for the duration of each
    /// event's dispatch, with <see cref="IBenzeneInvocation.InvocationId"/> set to the event's
    /// service-assigned <c>SequenceNumber</c>.
    /// </summary>
    /// <remarks>
    /// Each event is dispatched through its own DI scope (<c>EventHubConsumerApplication</c>'s
    /// per-event scope), which has no ambient <see cref="IBenzeneInvocation"/> of its own - a
    /// long-running worker has no Functions-style outer "invocation" boundary at all, so this is the
    /// only invocation identity available here. Auto-wired by <c>UseEventHub(...)</c> as the first
    /// middleware in the Event Hub pipeline, so no application code changes are required.
    /// </remarks>
    /// <param name="app">The pipeline builder to add the invocation middleware to.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<EventHubConsumerContext> UseBenzeneInvocation(
        this IMiddlewarePipelineBuilder<EventHubConsumerContext> app)
    {
        return app.UseBenzeneInvocation((_, context) =>
            new BenzeneInvocation(
                context.EventData.SequenceNumber.ToString(CultureInfo.InvariantCulture),
                WorkerApplicationBuilder.PlatformName,
                new Dictionary<Type, object>()));
    }
}
