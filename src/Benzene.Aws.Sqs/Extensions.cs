using System;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Aws.Core.AwsEventStream;

namespace Benzene.Aws.Sqs;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseSqs(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<SqsMessageContext>> action)
    {
        app.Register(x => x.AddSqs());
        var middlewarePipelineBuilder = app.Create<SqsMessageContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        return app.Use(resolver => new SqsLambdaHandler(new SqsApplication(pipeline), resolver));
    }
}
