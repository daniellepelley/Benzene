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
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseKafka(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<KafkaContext>> action)
    {
        app.Register(x => x.AddKafka());
        var pipeline = app.CreateMiddlewarePipeline(action);
        return app.Use(resolver => new KafkaLambdaHandler(new KafkaApplication(pipeline), resolver));
    }
}
