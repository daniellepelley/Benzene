using System;
using System.Collections.Generic;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.Correlation;
using Benzene.Core.Middleware;
using Benzene.Core.MiddlewareBuilder;

namespace Benzene.Aws.ApiGateway.ApiGatewayCustomAuthorizer;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseApiGatewayCustomAuthorizer(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<ApiGatewayCustomAuthorizerContext>> action)
    {
        var middlewarePipelineBuilder = app.Create<ApiGatewayCustomAuthorizerContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.AsPipeline();
        return app.Use(resolver => new ApiGatewayCustomAuthorizerLambdaHandler(pipeline, resolver));
    }

}
