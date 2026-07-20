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
    /// <param name="configure">
    /// Optionally configures <see cref="EventBridgeOptions"/> - e.g. set
    /// <see cref="EventBridgeOptions.RaiseOnFailureStatus"/> to escalate a non-exception failure result
    /// into a thrown exception so the EventBridge rule target retries it, or
    /// <see cref="EventBridgeOptions.CatchExceptions"/> to swallow handler exceptions instead of
    /// cascading them.
    /// </param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseEventBridge(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<EventBridgeContext>> action, Action<EventBridgeOptions> configure = null)
    {
        app.Register(x => x.AddEventBridge());
        var pipeline = app.CreateMiddlewarePipeline(action);
        var options = new EventBridgeOptions();
        configure?.Invoke(options);
        return app.Use(resolver => new EventBridgeLambdaHandler(new EventBridgeApplication(pipeline, options), resolver));
    }
}
