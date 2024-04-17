using System;
using System.Collections.Generic;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.MiddlewareBuilder;

namespace Benzene.Aws.ApiGateway;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseApiGateway(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<ApiGatewayContext>> action)
    {
        app.Register(x => x.AddApiGateway());
        var middlewarePipelineBuilder = app.Create<ApiGatewayContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.AsPipeline();
        return app.Use(resolver => new ApiGatewayLambdaHandler(pipeline, resolver));
    }


    public static void EnsureResponseExists(this ApiGatewayContext context)
    {
        context.ApiGatewayProxyResponse ??= new APIGatewayProxyResponse();
        context.ApiGatewayProxyResponse.Headers ??= new Dictionary<string, string>();
    }
}
