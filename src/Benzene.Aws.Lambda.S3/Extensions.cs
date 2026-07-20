using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Aws.S3
{
    /// <summary>
    /// Provides extension methods for adding S3 event notification handling to an AWS Lambda middleware pipeline.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Adds S3 event notification handling to the pipeline.
        /// </summary>
        /// <param name="app">The AWS event stream pipeline builder to add S3 handling to.</param>
        /// <param name="action">The action that configures the inner S3 pipeline.</param>
        /// <param name="configure">
        /// Optionally configures <see cref="S3Options"/> - e.g. set
        /// <see cref="S3Options.RaiseOnFailureStatus"/> to escalate a non-exception failure result into
        /// a thrown exception so S3's async-invoke retry applies, <see cref="S3Options.CatchExceptions"/>
        /// to swallow handler exceptions, or <see cref="S3Options.MaxDegreeOfParallelism"/> to cap the
        /// batch fan-out concurrency.
        /// </param>
        /// <returns>The pipeline builder for method chaining.</returns>
        public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseS3(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<S3RecordContext>> action, Action<S3Options> configure = null)
        {
            app.Register(x => x.AddS3());
            var pipeline = app.CreateMiddlewarePipeline(action);
            var options = new S3Options();
            configure?.Invoke(options);
            return app.Use(resolver => new S3LambdaHandler(new S3Application(pipeline, options), resolver));
        }
    }
}
