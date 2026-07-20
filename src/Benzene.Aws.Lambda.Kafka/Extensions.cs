using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Provides extension methods for adding Kafka handling to an AWS Lambda middleware pipeline.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds Kafka handling to the pipeline.
    /// </summary>
    /// <param name="app">The AWS event stream pipeline builder to add Kafka handling to.</param>
    /// <param name="action">The action that configures the inner Kafka pipeline.</param>
    /// <param name="configure">
    /// Optionally configures <see cref="KafkaOptions"/> — e.g. set
    /// <see cref="KafkaOptions.BatchFailureMode"/> to <see cref="KafkaBatchFailureMode.FailWholeBatch"/>
    /// to fail the whole batch on any record failure instead of the default per-partition
    /// partial-batch-failure reporting, or set <see cref="KafkaOptions.MaxDegreeOfParallelism"/> to
    /// bound how many topic-partitions run concurrently.
    /// </param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseKafka(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<KafkaContext>> action, Action<KafkaOptions> configure = null)
    {
        app.Register(x => x.AddKafka());
        var pipeline = app.CreateMiddlewarePipeline<KafkaContext>(builder =>
        {
            builder.UseBenzeneInvocation();
            action(builder);
        });
        var options = new KafkaOptions();
        configure?.Invoke(options);
        return app.Use(resolver => new KafkaLambdaHandler(new KafkaApplication(pipeline, options), resolver));
    }
}
