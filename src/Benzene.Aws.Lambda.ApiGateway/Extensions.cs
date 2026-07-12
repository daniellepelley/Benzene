using System;
using System.Collections.Generic;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Provides extension methods for adding API Gateway handling to an AWS Lambda middleware pipeline.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds API Gateway handling to the pipeline.
    /// </summary>
    /// <param name="app">The AWS event stream pipeline builder to add API Gateway handling to.</param>
    /// <param name="action">The action that configures the inner API Gateway pipeline.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseApiGateway(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<ApiGatewayContext>> action)
    {
        app.Register(x => x.AddApiGateway());
        var middlewarePipelineBuilder = app.Create<ApiGatewayContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        return app.Use(resolver => new ApiGatewayLambdaHandler(pipeline, resolver));
    }

    /// <summary>
    /// Ensures the context's API Gateway response and its headers dictionary are initialized.
    /// </summary>
    /// <param name="context">The API Gateway context to initialize the response on.</param>
    public static void EnsureResponseExists(this ApiGatewayContext context)
    {
        context.ApiGatewayProxyResponse ??= new APIGatewayProxyResponse();
        context.ApiGatewayProxyResponse.Headers ??= new Dictionary<string, string>();
    }
}
