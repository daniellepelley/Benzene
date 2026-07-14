using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.EventBridge;

/// <summary>
/// Provides extension methods for adding EventBridge handling to an AWS Lambda middleware pipeline.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds EventBridge handling to the pipeline. Payloads carrying <c>detail-type</c> and <c>source</c>
    /// are routed through the inner EventBridge pipeline (topic = <c>detail-type</c>, body = <c>detail</c>);
    /// anything else falls through to the next event source adapter.
    /// </summary>
    /// <param name="app">The AWS event stream pipeline builder to add EventBridge handling to.</param>
    /// <param name="action">The action that configures the inner EventBridge pipeline.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseEventBridge(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<EventBridgeContext>> action)
    {
        app.Register(x => x.AddEventBridge());
        var pipeline = app.CreateMiddlewarePipeline(action);
        return app.Use(resolver => new EventBridgeLambdaHandler(new EventBridgeApplication(pipeline), resolver));
    }
}
