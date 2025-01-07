using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Sqs;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseSqs(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<SqsMessageContext>> action)
    {
        app.Register(x => x.AddSqs());
        var pipeline = app.CreateMiddlewarePipeline(action);
        return app.Use(resolver => new SqsLambdaHandler(new SqsApplication(pipeline), resolver));
    }
}
