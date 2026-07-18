using System;
using System.Collections.Generic;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// Provides the per-message <see cref="IBenzeneInvocation"/> for the SQS pipeline.
/// </summary>
public static class BenzeneInvocationExtensions
{
    /// <summary>
    /// Adds middleware that exposes an <see cref="IBenzeneInvocation"/> for the duration of each SQS
    /// record's dispatch, with <see cref="IBenzeneInvocation.InvocationId"/> set to the record's SQS
    /// <c>MessageId</c>.
    /// </summary>
    /// <remarks>
    /// <see cref="Benzene.Aws.Lambda.Core.BenzeneInvocationExtensions.UseBenzeneInvocation(IMiddlewarePipelineBuilder{Benzene.Aws.Lambda.Core.AwsEventStream.AwsEventStreamContext})"/>
    /// populates <see cref="IBenzeneInvocation"/> for the whole Lambda invocation, but each SQS record
    /// is dispatched through its own DI scope (<c>SqsApplication</c>'s per-record
    /// <c>serviceResolverFactory.CreateScope()</c>) - a fresh scope has no populated
    /// <see cref="IBenzeneInvocation"/> of its own, so without this, resolving it inside a message
    /// handler throws (or, via <c>TryGetService</c>, silently comes back <see langword="null"/> - see
    /// <c>Benzene.Diagnostics.EnrichmentExtensions.UseBenzeneEnrichment</c>'s <c>invocationId</c> log
    /// enrichment, which is exactly what this fixes). This is auto-wired by <c>UseSqs(...)</c> as the
    /// first middleware in the SQS pipeline, so no application code changes are required.
    /// </remarks>
    /// <param name="app">The pipeline builder to add the invocation middleware to.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<SqsMessageContext> UseBenzeneInvocation(
        this IMiddlewarePipelineBuilder<SqsMessageContext> app)
    {
        return app.UseBenzeneInvocation((_, context) =>
            new BenzeneInvocation(context.SqsMessage.MessageId, Benzene.Aws.Lambda.Core.BenzeneInvocationExtensions.PlatformName, new Dictionary<Type, object>()));
    }
}
