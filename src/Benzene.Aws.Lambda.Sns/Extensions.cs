using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Sns;

/// <summary>
/// Provides extension methods for adding SNS handling to an AWS Lambda middleware pipeline.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds SNS handling to the pipeline.
    /// </summary>
    /// <param name="app">The AWS event stream pipeline builder to add SNS handling to.</param>
    /// <param name="action">The action that configures the inner SNS pipeline.</param>
    /// <param name="configure">
    /// Optionally configures <see cref="SnsOptions"/> - e.g. set <see cref="SnsOptions.CatchExceptions"/>
    /// to swallow handler exceptions instead of the default cascade-to-Lambda-invocation-failure
    /// behavior, or <see cref="SnsOptions.RaiseOnFailureStatus"/> to escalate a non-exception failure
    /// result into a thrown exception so SNS retries it too.
    /// </param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseSns(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<SnsRecordContext>> action, Action<SnsOptions> configure = null)
    {
        app.Register(x => x.AddSns());
        var pipeline = app.CreateMiddlewarePipeline<SnsRecordContext>(builder =>
        {
            builder.UseBenzeneInvocation();
            action(builder);
        });
        var options = new SnsOptions();
        configure?.Invoke(options);
        return app.Use(resolver => new SnsLambdaHandler(new SnsApplication(pipeline, options), resolver));
    }
}
