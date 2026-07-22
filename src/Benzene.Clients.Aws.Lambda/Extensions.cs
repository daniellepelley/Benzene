using System;
using Amazon.Lambda;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.HealthChecks.Core;
using Microsoft.Extensions.Logging;
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

    /// <summary>
    /// Adds an AWS Lambda function health check. By default (<see cref="HealthCheckMode.Reachability"/>) this
    /// is a non-destructive read-only <c>GetFunctionConfiguration</c> probe; pass
    /// <see cref="HealthCheckMode.Active"/> to really invoke the function with a ping instead (side-effecting).
    /// </summary>
    /// <param name="builder">The health check builder to add the check to.</param>
    /// <param name="lambdaName">The name of the Lambda function to check.</param>
    /// <param name="mode">Reachability (default, read-only) or Active (invokes the function — side-effecting).</param>
    /// <returns>The health check builder for method chaining.</returns>
    public static IHealthCheckBuilder AddLambdaHealthCheck(this IHealthCheckBuilder builder, string lambdaName, HealthCheckMode mode = HealthCheckMode.Reachability)
    {
        return builder.AddHealthCheck(resolver => new AwsLambdaHealthCheck(lambdaName, resolver.GetService<IAmazonLambda>(), resolver.GetService<ILogger<AwsLambdaHealthCheck>>(), mode));
    }
}
