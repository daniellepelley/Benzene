using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Aws.EventBridge
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
        /// <returns>The pipeline builder for method chaining.</returns>
        public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseS3(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<S3RecordContext>> action)
        {
            app.Register(x => x.AddS3());
            var pipeline = app.CreateMiddlewarePipeline(action);
            return app.Use(resolver => new S3LambdaHandler(new S3Application(pipeline), resolver));
        }
    }
}
