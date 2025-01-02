using System;
using Amazon.SQS;
using Benzene.Abstractions.Middleware;
using Benzene.Clients.Aws.Common;
using Benzene.Core.Middleware;

namespace Benzene.Clients.Aws.Sqs;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<SqsSendMessageContext> UseSqsClient(
        this IMiddlewarePipelineBuilder<SqsSendMessageContext> app, IAmazonSQS amazonSqs)
    {
        return app.Use(_ => new SqsClientMiddleware(amazonSqs));
    }
 
    public static IMiddlewarePipelineBuilder<SqsSendMessageContext> UseSqsClient(
        this IMiddlewarePipelineBuilder<SqsSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<SqsClientMiddleware>());
        return app.Use<SqsSendMessageContext, SqsClientMiddleware>();
    }
   
     public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
        IContextConverter<TContext, TContextOut> converter, IMiddlewarePipeline<TContextOut> middlewarePipeline)
    {
        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }

    public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
        IContextConverter<TContext, TContextOut> converter, Action<IMiddlewarePipelineBuilder<TContextOut>> action)
    {
        var middlewarePipeline = app.CreateMiddlewarePipeline(action);

        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }
    
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Results.Void>> UseSqs<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Results.Void>> app,
        string queueUrl,
        Action<IMiddlewarePipelineBuilder<SqsSendMessageContext>> action)
    {
        return app.Convert(new SqsContextConverter<T>(queueUrl), action);
    }
    
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Results.Void>> UseSqs<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Results.Void>> app, string queueUrl)
    {
        return app.Convert(new SqsContextConverter<T>(queueUrl), builder => builder.UseSqsClient());
    }
}