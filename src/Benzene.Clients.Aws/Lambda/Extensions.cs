using System;
using Amazon.Lambda;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.Lambda;

/// <summary>
/// Provides extension methods for wiring <see cref="AwsLambdaClientMiddleware"/> into middleware pipelines.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds an <see cref="AwsLambdaClientMiddleware"/> built from the given Lambda client to the pipeline.
    /// </summary>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="amazonLambda">The Lambda client used to invoke functions.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<LambdaSendMessageContext> UseAwsLambdaClient(
        this IMiddlewarePipelineBuilder<LambdaSendMessageContext> app, IAmazonLambda amazonLambda)
    {
        return app.Use(_ => new AwsLambdaClientMiddleware(amazonLambda));
    }

    /// <summary>
    /// Adds an <see cref="AwsLambdaClientMiddleware"/> resolved from the service container to the pipeline.
    /// </summary>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<LambdaSendMessageContext> UseAwsLambdaClient(
        this IMiddlewarePipelineBuilder<LambdaSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<AwsLambdaClientMiddleware>());
        return app.Use<LambdaSendMessageContext, AwsLambdaClientMiddleware>();
    }

    /// <summary>
    /// Converts the pipeline to invoke via AWS Lambda, using a custom middleware configuration.
    /// </summary>
    /// <typeparam name="T">The type of the outgoing message.</typeparam>
    /// <param name="app">The pipeline builder to convert.</param>
    /// <param name="action">A callback used to configure the converted Lambda invocation pipeline.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseAwsLambda<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app,
        Action<IMiddlewarePipelineBuilder<LambdaSendMessageContext>> action)
    {
        return app.Convert(new LambdaContextConverter<T>(), action);
    }

    /// <summary>
    /// Converts the pipeline to invoke via AWS Lambda, using the default <see cref="AwsLambdaClientMiddleware"/> configuration.
    /// </summary>
    /// <typeparam name="T">The type of the outgoing message.</typeparam>
    /// <param name="app">The pipeline builder to convert.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseAwsLambda<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app)
    {
        return app.Convert(new LambdaContextConverter<T>(), builder => builder.UseAwsLambdaClient());
    }
}
