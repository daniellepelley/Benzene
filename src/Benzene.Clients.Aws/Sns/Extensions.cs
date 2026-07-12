using System;
using Amazon.SimpleNotificationService;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.Sns;

/// <summary>
/// Provides extension methods for adding SNS publishing to a middleware pipeline.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds SNS publishing to the pipeline using an explicit SNS client instance.
    /// </summary>
    /// <param name="app">The pipeline builder to add SNS publishing to.</param>
    /// <param name="amazonSns">The SNS client to publish with.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<SnsSendMessageContext> UseSnsClient(
        this IMiddlewarePipelineBuilder<SnsSendMessageContext> app, IAmazonSimpleNotificationService amazonSns)
    {
        return app.Use(_ => new SnsClientMiddleware(amazonSns));
    }

    /// <summary>
    /// Adds SNS publishing to the pipeline, resolving the SNS client from dependency injection.
    /// </summary>
    /// <param name="app">The pipeline builder to add SNS publishing to.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<SnsSendMessageContext> UseSnsClient(
        this IMiddlewarePipelineBuilder<SnsSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<SnsClientMiddleware>());
        return app.Use<SnsSendMessageContext, SnsClientMiddleware>();
    }

    /// <summary>
    /// Converts the pipeline's context type using the given converter, configuring the inner pipeline inline.
    /// </summary>
    /// <typeparam name="TContext">The input context type.</typeparam>
    /// <typeparam name="TContextOut">The output context type.</typeparam>
    /// <param name="app">The pipeline builder to add the converter to.</param>
    /// <param name="converter">The context converter to use.</param>
    /// <param name="action">The action that configures the inner pipeline.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
        IContextConverter<TContext, TContextOut> converter, Action<IMiddlewarePipelineBuilder<TContextOut>> action)
    {
        var middlewarePipeline = app.CreateMiddlewarePipeline(action);
        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }

    /// <summary>
    /// Converts a Benzene client pipeline to publish to SNS, configuring the inner pipeline inline.
    /// </summary>
    /// <typeparam name="T">The message payload type being sent.</typeparam>
    /// <param name="app">The client pipeline builder to add SNS publishing to.</param>
    /// <param name="queueUrl">The ARN of the SNS topic to publish to.</param>
    /// <param name="action">The action that configures the inner SNS pipeline.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseSns<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app,
        string queueUrl, Action<IMiddlewarePipelineBuilder<SnsSendMessageContext>> action)
    {
        return Convert(app, new SnsContextConverter<T>(queueUrl), action);
    }

    /// <summary>
    /// Converts a Benzene client pipeline to publish to SNS, resolving the SNS client from dependency injection.
    /// </summary>
    /// <typeparam name="T">The message payload type being sent.</typeparam>
    /// <param name="app">The client pipeline builder to add SNS publishing to.</param>
    /// <param name="queueUrl">The ARN of the SNS topic to publish to.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseSns<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app, string queueUrl)
    {
        return app.Convert(new SnsContextConverter<T>(queueUrl), builder => builder.UseSnsClient());
    }
}
