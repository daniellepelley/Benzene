using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.Lambda.ApiGateway.ApiGatewayCustomAuthorizer;

/// <summary>
/// Provides extension methods for adding API Gateway custom authorizer handling to an AWS Lambda
/// middleware pipeline.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds API Gateway custom authorizer handling to the pipeline.
    /// </summary>
    /// <param name="app">The AWS event stream pipeline builder to add custom authorizer handling to.</param>
    /// <param name="action">The action that configures the inner custom authorizer pipeline.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseApiGatewayCustomAuthorizer(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<ApiGatewayCustomAuthorizerContext>> action)
    {
        var middlewarePipelineBuilder = app.Create<ApiGatewayCustomAuthorizerContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        return app.Use(resolver => new ApiGatewayCustomAuthorizerLambdaHandler(pipeline, resolver));
    }
}
