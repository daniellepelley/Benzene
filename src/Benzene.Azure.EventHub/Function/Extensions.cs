using Azure.Messaging.EventHubs;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Core;
using Benzene.Core.DI;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;

namespace Benzene.Azure.EventHub.Function;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<EventHubContext> UseBenzeneMessage(this IMiddlewarePipelineBuilder<EventHubContext> app, Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>> action)
    {
        app.Register(x => x.AddBenzeneMessage());
        var middlewarePipelineBuilder = app.Create<BenzeneMessageContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        return app.Use(resolver => new BenzeneMessageLambdaHandler(pipeline, resolver));
    }

    public static IMiddlewarePipelineBuilder<EventHubContext> UseBenzeneMessage(this IMiddlewarePipelineBuilder<EventHubContext> app, IMiddlewarePipelineBuilder<BenzeneMessageContext> builder)
    {
        var pipeline = builder.Build();
        return app.Use(resolver => new BenzeneMessageLambdaHandler(pipeline, resolver));
    }

    public static Task HandleEventHub(this IAzureFunctionApp source, params EventData[] eventData)
    {
        return source.HandleAsync(eventData);
    }

}
