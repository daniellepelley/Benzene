using System;
using Amazon.Lambda;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.Middleware;

namespace Benzene.Clients.Aws.Lambda;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<LambdaSendMessageContext> UseAwsLambdaClient(
        this IMiddlewarePipelineBuilder<LambdaSendMessageContext> app, IAmazonLambda amazonLambda)
    {
        return app.Use(_ => new AwsLambdaClientMiddleware(amazonLambda));
    }
 
    public static IMiddlewarePipelineBuilder<LambdaSendMessageContext> UseAwsLambdaClient(
        this IMiddlewarePipelineBuilder<LambdaSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<AwsLambdaClientMiddleware>());
        return app.Use<LambdaSendMessageContext, AwsLambdaClientMiddleware>();
    }
   
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Results.Void>> UseAwsLambda<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Results.Void>> app,
        Action<IMiddlewarePipelineBuilder<LambdaSendMessageContext>> action)
    {
        return app.Convert(new LambdaContextConverter<T>(), action);
    }
    
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Results.Void>> UseAwsLambda<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Results.Void>> app)
    {
        return app.Convert(new LambdaContextConverter<T>(), builder => builder.UseAwsLambdaClient());
    }
}