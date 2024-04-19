using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.DI;
using Benzene.Core.DirectMessage;

namespace Benzene.Azure.Core.EventHub;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<EventHubContext> UseDirectMessage(this IMiddlewarePipelineBuilder<EventHubContext> app, Action<IMiddlewarePipelineBuilder<DirectMessageContext>> action)
    {
        app.Register(x => x.AddDirectMessage());
        var middlewarePipelineBuilder = app.Create<DirectMessageContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        return app.Use(resolver => new DirectMessageLambdaHandler(pipeline, resolver));
    }

    public static IMiddlewarePipelineBuilder<EventHubContext> UseDirectMessage(this IMiddlewarePipelineBuilder<EventHubContext> app, IMiddlewarePipelineBuilder<DirectMessageContext> builder)
    {
        var pipeline = builder.Build();
        return app.Use(resolver => new DirectMessageLambdaHandler(pipeline, resolver));
    }
}
