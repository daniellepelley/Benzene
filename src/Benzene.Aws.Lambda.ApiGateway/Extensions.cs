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
    /// Adds API Gateway HTTP API (payload format version 2.0) handling to the pipeline. Register this
    /// alongside <see cref="UseApiGateway"/> to serve REST (v1) and HTTP API (v2) front doors from one
    /// Lambda — the two routers are mutually exclusive on payload shape, so each claims only its own
    /// events regardless of registration order.
    /// </summary>
    /// <param name="app">The AWS event stream pipeline builder to add API Gateway v2 handling to.</param>
    /// <param name="action">The action that configures the inner API Gateway v2 pipeline.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseApiGatewayV2(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<ApiGatewayV2Context>> action)
    {
        app.Register(x => x.AddApiGatewayV2());
        var middlewarePipelineBuilder = app.Create<ApiGatewayV2Context>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        return app.Use(resolver => new ApiGatewayV2LambdaHandler(pipeline, resolver));
    }

    /// <summary>
    /// Ensures the context's API Gateway response and its headers dictionary are initialized.
    /// </summary>
    /// <param name="context">The API Gateway context to initialize the response on.</param>
    public static void EnsureResponseExists(this ApiGatewayContext context)
    {
        context.ApiGatewayProxyResponse ??= new APIGatewayProxyResponse();
        context.ApiGatewayProxyResponse.Headers ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures the context's API Gateway v2 response and its headers dictionary are initialized.
    /// </summary>
    /// <param name="context">The API Gateway v2 context to initialize the response on.</param>
    public static void EnsureResponseExists(this ApiGatewayV2Context context)
    {
        context.ApiGatewayProxyResponse ??= new APIGatewayHttpApiV2ProxyResponse();
        context.ApiGatewayProxyResponse.Headers ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
