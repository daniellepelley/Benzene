using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Aws.EventBridge
{
    public static class Extensions
    {
        public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseS3(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<S3RecordContext>> action)
        {
            app.Register(x => x.AddS3());
            var pipeline = app.CreateMiddlewarePipeline(action);
            return app.Use(resolver => new S3LambdaHandler(new S3Application(pipeline), resolver));
        }
    }
}
