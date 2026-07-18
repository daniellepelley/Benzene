using System;
using System.Collections.Generic;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.Core;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.Kafka;

/// <summary>
/// Provides the per-record <see cref="IBenzeneInvocation"/> for the Kafka trigger's pipeline.
/// </summary>
public static class BenzeneInvocationExtensions
{
    /// <summary>
    /// Adds middleware that exposes an <see cref="IBenzeneInvocation"/> for the duration of each
    /// record's dispatch, with <see cref="IBenzeneInvocation.InvocationId"/> set to
    /// <c>"{topic}-{partition}-{offset}"</c> - Kafka records have no single message-id field, but this
    /// triple uniquely identifies a record.
    /// </summary>
    /// <remarks>
    /// Each record in the trigger's batch is dispatched through its own DI scope
    /// (<c>KafkaApplication</c>'s <c>MiddlewareMultiApplication</c>-driven per-record
    /// <c>serviceResolverFactory.CreateScope()</c>), which doesn't inherit whatever
    /// <see cref="IBenzeneInvocation"/> the outer Azure Functions invocation populated. Auto-wired by
    /// <c>UseKafka(...)</c> as the first middleware in the Kafka pipeline, so no application code
    /// changes are required.
    /// </remarks>
    /// <param name="app">The pipeline builder to add the invocation middleware to.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<KafkaContext> UseBenzeneInvocation(
        this IMiddlewarePipelineBuilder<KafkaContext> app)
    {
        return app.UseBenzeneInvocation((_, context) =>
            new BenzeneInvocation(
                $"{context.KafkaEvent.Topic}-{context.KafkaEvent.Partition}-{context.KafkaEvent.Offset}",
                AzureFunctionAppBuilder.PlatformName,
                new Dictionary<Type, object>()));
    }
}
