using System;
using System.Collections.Generic;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.SelfHost;

namespace Benzene.Kafka.Core.KafkaMessage;

/// <summary>
/// Provides the per-message <see cref="IBenzeneInvocation"/> for the self-hosted Kafka consumer
/// worker's pipeline.
/// </summary>
public static class BenzeneInvocationExtensions
{
    /// <summary>
    /// Adds middleware that exposes an <see cref="IBenzeneInvocation"/> for the duration of each
    /// consumed record's dispatch, with <see cref="IBenzeneInvocation.InvocationId"/> set to
    /// <c>"{topic}-{partition}-{offset}"</c> - Kafka records have no single message-id field, but this
    /// triple uniquely identifies a record.
    /// </summary>
    /// <remarks>
    /// Each record is dispatched through its own DI scope
    /// (<c>BoundedConcurrentDispatcher</c>'s per-record scope, via <c>KafkaApplication</c>), which has
    /// no ambient <see cref="IBenzeneInvocation"/> of its own - a long-running worker has no
    /// Lambda/Functions-style outer "invocation" boundary at all, so this is the only invocation
    /// identity available here. Auto-wired by <c>UseKafka(...)</c> as the first middleware in the
    /// Kafka pipeline, so no application code changes are required.
    /// </remarks>
    /// <param name="app">The pipeline builder to add the invocation middleware to.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<KafkaRecordContext<TKey, TValue>> UseBenzeneInvocation<TKey, TValue>(
        this IMiddlewarePipelineBuilder<KafkaRecordContext<TKey, TValue>> app)
    {
        return app.UseBenzeneInvocation((_, context) =>
            new BenzeneInvocation(
                $"{context.ConsumeResult.Topic}-{context.ConsumeResult.Partition.Value}-{context.ConsumeResult.Offset.Value}",
                WorkerApplicationBuilder.PlatformName,
                new Dictionary<Type, object>()));
    }
}
