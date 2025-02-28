using System;
using Amazon.SimpleNotificationService;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.Sns;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<SnsSendMessageContext> UseSnsClient(
        this IMiddlewarePipelineBuilder<SnsSendMessageContext> app, IAmazonSimpleNotificationService amazonSns)
    {
        return app.Use(_ => new SnsClientMiddleware(amazonSns));
    }
 
    public static IMiddlewarePipelineBuilder<SnsSendMessageContext> UseSnsClient(
        this IMiddlewarePipelineBuilder<SnsSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<SnsClientMiddleware>());
        return app.Use<SnsSendMessageContext, SnsClientMiddleware>();
    }
   
    public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
        IContextConverter<TContext, TContextOut> converter, Action<IMiddlewarePipelineBuilder<TContextOut>> action)
    {
        var middlewarePipeline = app.CreateMiddlewarePipeline(action);
        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }
    
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseSns<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app,
        string queueUrl, Action<IMiddlewarePipelineBuilder<SnsSendMessageContext>> action)
    {
        return Convert(app, new SnsContextConverter<T>(queueUrl), action);
    }
    
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseSns<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app, string queueUrl)
    {
        return app.Convert(new SnsContextConverter<T>(queueUrl), builder => builder.UseSnsClient());
    }
}