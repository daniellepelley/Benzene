using System;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.MiddlewareBuilder;

namespace Benzene.Aws.Sns;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseSns(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<SnsRecordContext>> action)
    {
        app.Register(x => x.AddSns());
        var middlewarePipelineBuilder = app.Create<SnsRecordContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.AsPipeline();
        return app.Use(resolver => new SnsLambdaHandler(new SnsApplication(pipeline), resolver));
    }
}
