using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Core.BenzeneMessage;

/// <summary>
/// Provides extension methods for adding BenzeneMessage handling to an AWS Lambda middleware pipeline.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds BenzeneMessage handling to the pipeline, configuring the inner pipeline inline.
    /// </summary>
    /// <param name="app">The AWS event stream pipeline builder to add BenzeneMessage handling to.</param>
    /// <param name="action">The action that configures the inner BenzeneMessage pipeline.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseBenzeneMessage(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>> action)
    {
        app.Register(x => x.AddBenzeneMessage());
        var pipeline = app.CreateMiddlewarePipeline(action);
        return app.Use(resolver => new BenzeneMessageLambdaHandler(pipeline, resolver));
    }

    /// <summary>
    /// Adds BenzeneMessage handling to the pipeline using an already-configured inner pipeline builder.
    /// </summary>
    /// <param name="app">The AWS event stream pipeline builder to add BenzeneMessage handling to.</param>
    /// <param name="builder">The pre-configured BenzeneMessage pipeline builder to build and use.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This overload is used when the same BenzeneMessage pipeline needs to be shared across multiple
    /// event source adapters (e.g. both <c>UseBenzeneMessage</c> and <c>UseApiGateway</c>'s
    /// <c>UseHttpToBenzeneMessage</c>), so it's built once and reused rather than reconfigured per adapter.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseBenzeneMessage(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IMiddlewarePipelineBuilder<BenzeneMessageContext> builder)
    {
        var pipeline = builder.Build();
        return app.Use(resolver => new BenzeneMessageLambdaHandler(pipeline, resolver));
    }
}
