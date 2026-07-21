using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Kinesis;

/// <summary>
/// Provides extension methods for adding Kinesis Data Streams handling to an AWS Lambda middleware pipeline.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds Kinesis Data Streams handling to the pipeline as a stream (fan-in): the whole batch is
    /// exposed to the inner pipeline as one <see cref="StreamContext{TItem}"/> of records, consumed with
    /// <c>UseStream(...)</c>. Invocations whose first record's source is <c>aws:kinesis</c> are handled
    /// here; anything else falls through to the next event source adapter.
    /// </summary>
    /// <param name="app">The AWS event stream pipeline builder to add Kinesis handling to.</param>
    /// <param name="action">The action that configures the inner Kinesis stream pipeline.</param>
    /// <param name="options">
    /// Checkpointing options. Defaults to a new <see cref="KinesisStreamOptions"/>
    /// (<see cref="KinesisStreamOptions.AutoCheckpointOnSuccess"/> on) if omitted.
    /// </param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// app.UseKinesisStream(kinesis => kinesis
    ///     .UseStream&lt;KinesisEventRecord&gt;(async (records, ct) =>
    ///     {
    ///         await foreach (var partition in records.PartitionBy(r => r.Kinesis.PartitionKey, ct))
    ///         {
    ///             // partition-ordered records
    ///         }
    ///     }));
    /// </code>
    /// </example>
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseKinesisStream(
        this IMiddlewarePipelineBuilder<AwsEventStreamContext> app,
        Action<IMiddlewarePipelineBuilder<StreamContext<KinesisEventRecord>>> action,
        KinesisStreamOptions options = null)
    {
        app.Register(x => x.AddKinesis());
        var pipeline = app.CreateMiddlewarePipeline(action);
        return app.Use(resolver => new KinesisLambdaHandler(new KinesisStreamApplication(pipeline, options), resolver));
    }
}
