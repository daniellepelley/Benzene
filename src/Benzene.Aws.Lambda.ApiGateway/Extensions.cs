using System;
using System.Collections.Generic;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.ApiGateway;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseApiGateway(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<ApiGatewayContext>> action)
    {
        app.Register(x => x.AddApiGateway());
        var middlewarePipelineBuilder = app.Create<ApiGatewayContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        return app.Use(resolver => new ApiGatewayLambdaHandler(pipeline, resolver));
    }


    public static void EnsureResponseExists(this ApiGatewayContext context)
    {
        context.ApiGatewayProxyResponse ??= new APIGatewayProxyResponse();
        context.ApiGatewayProxyResponse.Headers ??= new Dictionary<string, string>();
    }
}
