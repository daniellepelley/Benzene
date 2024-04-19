using System;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.DI;
using Benzene.Core.DirectMessage;

namespace Benzene.Aws.Core.DirectMessage;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseDirectMessage(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<DirectMessageContext>> action)
    {
        app.Register(x => x.AddDirectMessage());
        var middlewarePipelineBuilder = app.Create<DirectMessageContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        return app.Use(resolver => new DirectMessageLambdaHandler(pipeline, resolver));
    }
    
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseDirectMessage(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IMiddlewarePipelineBuilder<DirectMessageContext> builder)
    {
        var pipeline = builder.Build();
        return app.Use(resolver => new DirectMessageLambdaHandler(pipeline, resolver));
    }
}
