using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// Provides extension methods for adding SQS handling to an AWS Lambda middleware pipeline.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds SQS handling to the pipeline.
    /// </summary>
    /// <param name="app">The AWS event stream pipeline builder to add SQS handling to.</param>
    /// <param name="action">The action that configures the inner SQS pipeline.</param>
    /// <param name="configure">
    /// Optionally configures <see cref="SqsOptions"/> - e.g. set <see cref="SqsOptions.BatchFailureMode"/>
    /// to <see cref="SqsBatchFailureMode.FailWholeBatch"/> to fail the whole batch on any message
    /// failure instead of the default per-message partial-batch-failure reporting.
    /// </param>
    /// <param name="topicAttributeKey">
    /// The message attribute the topic is read from, defaulting to
    /// <see cref="SqsMessageTopicGetter.DefaultTopicAttribute"/> (<c>"topic"</c>). Pass a different key
    /// to consume messages a non-Benzene producer routes on another attribute.
    /// </param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseSqs(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<SqsMessageContext>> action, Action<SqsOptions> configure = null, string topicAttributeKey = SqsMessageTopicGetter.DefaultTopicAttribute)
    {
        app.Register(x => x.AddSqs(topicAttributeKey));
        var pipeline = app.CreateMiddlewarePipeline<SqsMessageContext>(builder =>
        {
            builder.UseBenzeneInvocation();
            action(builder);
        });
        var options = new SqsOptions();
        configure?.Invoke(options);
        return app.Use(resolver => new SqsLambdaHandler(new SqsApplication(pipeline, options), resolver));
    }
}
