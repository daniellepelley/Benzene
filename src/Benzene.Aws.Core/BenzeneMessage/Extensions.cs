using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.DI;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Core.BenzeneMessage;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseBenzeneMessage(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>> action)
    {
        app.Register(x => x.AddBenzeneMessage());
        var pipeline = app.CreateMiddlewarePipeline(action);
        return app.Use(resolver => new BenzeneMessageLambdaHandler(pipeline, resolver));
    }
    
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseBenzeneMessage(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IMiddlewarePipelineBuilder<BenzeneMessageContext> builder)
    {
        var pipeline = builder.Build();
        return app.Use(resolver => new BenzeneMessageLambdaHandler(pipeline, resolver));
    }
}
