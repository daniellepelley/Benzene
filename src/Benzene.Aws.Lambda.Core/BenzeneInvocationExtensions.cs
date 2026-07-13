using System;
using System.Collections.Generic;
using Amazon.Lambda.Core;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Core;

/// <summary>
/// Provides the AWS Lambda implementation of <see cref="IBenzeneInvocation"/>.
/// </summary>
public static class BenzeneInvocationExtensions
{
    /// <summary>The platform identifier reported by <see cref="IBenzeneInvocation.Platform"/> on AWS Lambda.</summary>
    public const string PlatformName = "AwsLambda";

    /// <summary>
    /// Adds middleware that exposes an <see cref="IBenzeneInvocation"/> for the duration of the
    /// invocation, with <see cref="IBenzeneInvocation.InvocationId"/> set to the Lambda request ID and
    /// <c>GetFeature&lt;ILambdaContext&gt;()</c> returning the native Lambda execution context.
    /// </summary>
    /// <param name="app">The pipeline builder to add the invocation middleware to.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseBenzeneInvocation(
        this IMiddlewarePipelineBuilder<AwsEventStreamContext> app)
    {
        return app.UseBenzeneInvocation((_, context) =>
        {
            var features = context.LambdaContext == null
                ? new Dictionary<Type, object>()
                : new Dictionary<Type, object> { [typeof(ILambdaContext)] = context.LambdaContext };

            return new BenzeneInvocation(context.LambdaContext?.AwsRequestId ?? Guid.NewGuid().ToString(), PlatformName, features);
        });
    }
}
