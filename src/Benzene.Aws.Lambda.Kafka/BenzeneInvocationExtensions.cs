using System;
using System.Collections.Generic;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Provides the per-message <see cref="IBenzeneInvocation"/> for the Kafka pipeline.
/// </summary>
public static class BenzeneInvocationExtensions
{
    /// <summary>
    /// Adds middleware that exposes an <see cref="IBenzeneInvocation"/> for the duration of each Kafka
    /// record's dispatch, with <see cref="IBenzeneInvocation.InvocationId"/> set to
    /// <c>"{topic}-{partition}-{offset}"</c> - Kafka records have no single message-id field, but this
    /// triple uniquely identifies a record.
    /// </summary>
    /// <remarks>
    /// Each Kafka record is dispatched through its own DI scope (fanned out by
    /// <c>MiddlewareMultiApplication</c>'s per-record <c>serviceResolverFactory.CreateScope()</c>),
    /// which doesn't inherit whatever <see cref="IBenzeneInvocation"/> was populated for the whole
    /// Lambda invocation - see
    /// <see cref="Benzene.Aws.Lambda.Sqs.BenzeneInvocationExtensions.UseBenzeneInvocation(IMiddlewarePipelineBuilder{Benzene.Aws.Lambda.Sqs.SqsMessageContext})"/>
    /// for the full rationale (identical shape, SQS side). Auto-wired by <c>UseKafka(...)</c> as the
    /// first middleware in the Kafka pipeline, so no application code changes are required.
    /// </remarks>
    /// <param name="app">The pipeline builder to add the invocation middleware to.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<KafkaContext> UseBenzeneInvocation(
        this IMiddlewarePipelineBuilder<KafkaContext> app)
    {
        return app.UseBenzeneInvocation((_, context) =>
            new BenzeneInvocation(
                $"{context.KafkaEventRecord.Topic}-{context.KafkaEventRecord.Partition}-{context.KafkaEventRecord.Offset}",
                Benzene.Aws.Lambda.Core.BenzeneInvocationExtensions.PlatformName,
                new Dictionary<Type, object>()));
    }
}
