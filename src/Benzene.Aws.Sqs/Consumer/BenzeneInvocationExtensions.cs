using System;
using System.Collections.Generic;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.SelfHost;

namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Provides the per-message <see cref="IBenzeneInvocation"/> for the self-hosted SQS polling
/// consumer's pipeline.
/// </summary>
public static class BenzeneInvocationExtensions
{
    /// <summary>
    /// Adds middleware that exposes an <see cref="IBenzeneInvocation"/> for the duration of each
    /// polled message's dispatch, with <see cref="IBenzeneInvocation.InvocationId"/> set to the
    /// message's SQS <c>MessageId</c>.
    /// </summary>
    /// <remarks>
    /// Each polled message is dispatched through its own DI scope
    /// (<c>SqsConsumerApplication</c>'s per-message scope), which has no ambient
    /// <see cref="IBenzeneInvocation"/> of its own - a long-running worker has no Lambda-style outer
    /// "invocation" boundary at all, so this is the only invocation identity available here. Auto-wired
    /// by <c>UseSqs(...)</c> as the first middleware in the SQS consumer pipeline, so no application
    /// code changes are required.
    /// </remarks>
    /// <param name="app">The pipeline builder to add the invocation middleware to.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<SqsConsumerMessageContext> UseBenzeneInvocation(
        this IMiddlewarePipelineBuilder<SqsConsumerMessageContext> app)
    {
        return app.UseBenzeneInvocation((_, context) =>
            new BenzeneInvocation(context.Message.MessageId, WorkerApplicationBuilder.PlatformName, new Dictionary<Type, object>()));
    }
}
