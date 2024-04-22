using System;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.DI;
using Benzene.Core.BenzeneMessage;

namespace Benzene.Aws.Core.BenzeneMessage;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseBenzeneMessage(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>> action)
    {
        app.Register(x => x.AddBenzeneMessage());
        var middlewarePipelineBuilder = app.Create<BenzeneMessageContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        return app.Use(resolver => new BenzeneMessageLambdaHandler(pipeline, resolver));
    }
    
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseBenzeneMessage(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IMiddlewarePipelineBuilder<BenzeneMessageContext> builder)
    {
        var pipeline = builder.Build();
        return app.Use(resolver => new BenzeneMessageLambdaHandler(pipeline, resolver));
    }
}
