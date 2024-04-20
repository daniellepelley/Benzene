using Azure.Messaging.EventHubs;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Azure.Core;
using Benzene.Core.DI;
using Benzene.Core.DirectMessage;

namespace Benzene.Azure.EventHub;

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
    
    public static Task HandleEventHub(this IAzureApp source, params EventData[] eventData)
    {
        return source.HandleAsync(eventData);
    }

}
