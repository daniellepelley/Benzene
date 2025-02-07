using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Sns;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseSns(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<SnsRecordContext>> action)
    {
        app.Register(x => x.AddSns());
        var pipeline = app.CreateMiddlewarePipeline(action);
        return app.Use(resolver => new SnsLambdaHandler(new SnsApplication(pipeline), resolver));
    }
}
