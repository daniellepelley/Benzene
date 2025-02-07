using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.Lambda.ApiGateway.ApiGatewayCustomAuthorizer;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseApiGatewayCustomAuthorizer(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<ApiGatewayCustomAuthorizerContext>> action)
    {
        var middlewarePipelineBuilder = app.Create<ApiGatewayCustomAuthorizerContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        return app.Use(resolver => new ApiGatewayCustomAuthorizerLambdaHandler(pipeline, resolver));
    }
}
