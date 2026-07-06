using System;
using Amazon.SQS;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Void = Benzene.Abstractions.Results.Void;

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
   
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseSqs<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app,
        string queueUrl,
        Action<IMiddlewarePipelineBuilder<SqsSendMessageContext>> action)
    {
        return app.Convert(new SqsContextConverter<T>(queueUrl), action);
    }
    
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseSqs<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app, string queueUrl)
    {
        return app.Convert(new SqsContextConverter<T>(queueUrl), builder => builder.UseSqsClient());
    }

    public static IBenzeneServiceContainer AddSqsMessageClient(this IBenzeneServiceContainer services, string queueUrl, Action<IMiddlewarePipelineBuilder<SqsSendMessageContext>> action)
    {
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<SqsSendMessageContext>(services);
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        
        services.AddScoped(x => new SqsBenzeneMessageClient(queueUrl, pipeline,
            x.GetService<IBenzeneLogger>(), x.GetService<IServiceResolver>()));
        return services;
    }
}