using System;
using Amazon.EventBridge;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.EventBridge;

/// <summary>
/// Provides pipeline-composition extensions for publishing to EventBridge, mirroring the SNS/SQS
/// client building blocks.
/// </summary>
public static class Extensions
{
    public static IMiddlewarePipelineBuilder<EventBridgeSendMessageContext> UseEventBridgeClient(
        this IMiddlewarePipelineBuilder<EventBridgeSendMessageContext> app, IAmazonEventBridge amazonEventBridge)
    {
        return app.Use(_ => new EventBridgeClientMiddleware(amazonEventBridge));
    }

    public static IMiddlewarePipelineBuilder<EventBridgeSendMessageContext> UseEventBridgeClient(
        this IMiddlewarePipelineBuilder<EventBridgeSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<EventBridgeClientMiddleware>());
        return app.Use<EventBridgeSendMessageContext, EventBridgeClientMiddleware>();
    }

    public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
        IContextConverter<TContext, TContextOut> converter, Action<IMiddlewarePipelineBuilder<TContextOut>> action)
    {
        var middlewarePipeline = app.CreateMiddlewarePipeline(action);
        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }

    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseEventBridge<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app,
        string source, Action<IMiddlewarePipelineBuilder<EventBridgeSendMessageContext>> action)
    {
        return Convert(app, new EventBridgeContextConverter<T>(source), action);
    }

    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseEventBridge<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app, string source)
    {
        return app.Convert(new EventBridgeContextConverter<T>(source), builder => builder.UseEventBridgeClient());
    }
}
