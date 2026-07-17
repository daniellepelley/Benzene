using System;
using Amazon.SQS;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.Sqs;

/// <summary>
/// Provides extension methods for wiring <see cref="SqsClientMiddleware"/> into middleware pipelines.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds a <see cref="SqsClientMiddleware"/> built from the given SQS client to the pipeline.
    /// </summary>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="amazonSqs">The SQS client used to send messages.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<SqsSendMessageContext> UseSqsClient(
        this IMiddlewarePipelineBuilder<SqsSendMessageContext> app, IAmazonSQS amazonSqs)
    {
        return app.Use(_ => new SqsClientMiddleware(amazonSqs));
    }

    /// <summary>
    /// Adds a <see cref="SqsClientMiddleware"/> resolved from the service container to the pipeline.
    /// </summary>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<SqsSendMessageContext> UseSqsClient(
        this IMiddlewarePipelineBuilder<SqsSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<SqsClientMiddleware>());
        return app.Use<SqsSendMessageContext, SqsClientMiddleware>();
    }

    /// <summary>
    /// Converts the pipeline to send via SQS, using a custom middleware configuration.
    /// </summary>
    /// <typeparam name="T">The type of the outgoing message.</typeparam>
    /// <param name="app">The pipeline builder to convert.</param>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <param name="action">A callback used to configure the converted SQS send pipeline.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseSqs<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app,
        string queueUrl,
        Action<IMiddlewarePipelineBuilder<SqsSendMessageContext>> action)
    {
        return app.Convert(new SqsContextConverter<T>(queueUrl), action);
    }

    /// <summary>
    /// Converts the pipeline to send via SQS, using the default <see cref="SqsClientMiddleware"/> configuration.
    /// </summary>
    /// <typeparam name="T">The type of the outgoing message.</typeparam>
    /// <param name="app">The pipeline builder to convert.</param>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseSqs<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app, string queueUrl)
    {
        return app.Convert(new SqsContextConverter<T>(queueUrl), builder => builder.UseSqsClient());
    }

    /// <summary>
    /// Converts an outbound route pipeline (<c>OutboundRoutingBuilder.Route</c>) to send via SQS,
    /// using a custom middleware configuration. See <c>work/benzene-clients-redesign-plan.md</c> §3.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to convert.</param>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <param name="action">A callback used to configure the converted SQS send pipeline.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseSqs(this IMiddlewarePipelineBuilder<OutboundContext> app,
        string queueUrl,
        Action<IMiddlewarePipelineBuilder<SqsSendMessageContext>> action)
    {
        return app.Convert(new OutboundSqsContextConverter(queueUrl), action);
    }

    /// <summary>
    /// Converts an outbound route pipeline (<c>OutboundRoutingBuilder.Route</c>) to send via SQS,
    /// using the default <see cref="SqsClientMiddleware"/> configuration.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to convert.</param>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseSqs(this IMiddlewarePipelineBuilder<OutboundContext> app, string queueUrl)
    {
        return app.Convert(new OutboundSqsContextConverter(queueUrl), builder => builder.UseSqsClient());
    }

    /// <summary>
    /// Registers a scoped <see cref="SqsBenzeneMessageClient"/> built from a custom middleware pipeline
    /// configuration.
    /// </summary>
    /// <param name="services">The service container to register on.</param>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <param name="action">A callback used to configure the SQS send pipeline.</param>
    /// <returns>The service container, for chaining.</returns>
    public static IBenzeneServiceContainer AddSqsMessageClient(this IBenzeneServiceContainer services, string queueUrl, Action<IMiddlewarePipelineBuilder<SqsSendMessageContext>> action)
    {
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<SqsSendMessageContext>(services);
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();

        services.AddScoped(x => new SqsBenzeneMessageClient(queueUrl, pipeline,
            x.GetService<ILogger<SqsBenzeneMessageClient>>(), x.GetService<IServiceResolver>()));
        return services;
    }
}
