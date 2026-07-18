using System;
using System.Collections.Generic;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Sns;

/// <summary>
/// Provides the per-message <see cref="IBenzeneInvocation"/> for the SNS pipeline.
/// </summary>
public static class BenzeneInvocationExtensions
{
    /// <summary>
    /// Adds middleware that exposes an <see cref="IBenzeneInvocation"/> for the duration of each SNS
    /// record's dispatch, with <see cref="IBenzeneInvocation.InvocationId"/> set to the record's SNS
    /// <c>MessageId</c>.
    /// </summary>
    /// <remarks>
    /// Each SNS record is dispatched through its own DI scope (<c>SnsApplication</c>'s per-record
    /// <c>serviceResolverFactory.CreateScope()</c>), which doesn't inherit whatever
    /// <see cref="IBenzeneInvocation"/> was populated for the whole Lambda invocation - see
    /// <see cref="Benzene.Aws.Lambda.Sqs.BenzeneInvocationExtensions.UseBenzeneInvocation(IMiddlewarePipelineBuilder{Benzene.Aws.Lambda.Sqs.SqsMessageContext})"/>
    /// for the full rationale (identical shape, SQS side). Auto-wired by <c>UseSns(...)</c> as the
    /// first middleware in the SNS pipeline, so no application code changes are required.
    /// </remarks>
    /// <param name="app">The pipeline builder to add the invocation middleware to.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<SnsRecordContext> UseBenzeneInvocation(
        this IMiddlewarePipelineBuilder<SnsRecordContext> app)
    {
        return app.UseBenzeneInvocation((_, context) =>
            new BenzeneInvocation(context.SnsRecord.Sns.MessageId, Benzene.Aws.Lambda.Core.BenzeneInvocationExtensions.PlatformName, new Dictionary<Type, object>()));
    }
}
