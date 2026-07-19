using System;
using Amazon.SQS;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.HealthChecks.Core;
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
    /// <param name="topicAttributeKey">The message attribute the topic is written to (defaults to <see cref="SqsContextConverter{T}.DefaultTopicAttribute"/>).</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseSqs<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app,
        string queueUrl,
        Action<IMiddlewarePipelineBuilder<SqsSendMessageContext>> action,
        string topicAttributeKey = SqsContextConverter<T>.DefaultTopicAttribute)
    {
        return app.Convert(new SqsContextConverter<T>(queueUrl, topicAttributeKey), action);
    }

    /// <summary>
    /// Converts the pipeline to send via SQS, using the default <see cref="SqsClientMiddleware"/> configuration.
    /// </summary>
    /// <typeparam name="T">The type of the outgoing message.</typeparam>
    /// <param name="app">The pipeline builder to convert.</param>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <param name="topicAttributeKey">The message attribute the topic is written to (defaults to <see cref="SqsContextConverter{T}.DefaultTopicAttribute"/>).</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseSqs<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app, string queueUrl, string topicAttributeKey = SqsContextConverter<T>.DefaultTopicAttribute)
    {
        return app.Convert(new SqsContextConverter<T>(queueUrl, topicAttributeKey), builder => builder.UseSqsClient());
    }

    /// <summary>
    /// Converts an outbound route pipeline (<c>OutboundRoutingBuilder.Route</c>) to send via SQS,
    /// using a custom middleware configuration. See <c>work/benzene-clients-redesign-plan.md</c> §3.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to convert.</param>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <param name="action">A callback used to configure the converted SQS send pipeline.</param>
    /// <param name="topicAttributeKey">The message attribute the topic is written to (defaults to <see cref="OutboundSqsContextConverter.DefaultTopicAttribute"/>).</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseSqs(this IMiddlewarePipelineBuilder<OutboundContext> app,
        string queueUrl,
        Action<IMiddlewarePipelineBuilder<SqsSendMessageContext>> action,
        string topicAttributeKey = OutboundSqsContextConverter.DefaultTopicAttribute)
    {
        return app.Convert(new OutboundSqsContextConverter(queueUrl, topicAttributeKey), action);
    }

    /// <summary>
    /// Converts an outbound route pipeline (<c>OutboundRoutingBuilder.Route</c>) to send via SQS,
    /// using the default <see cref="SqsClientMiddleware"/> configuration.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to convert.</param>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <param name="topicAttributeKey">The message attribute the topic is written to (defaults to <see cref="OutboundSqsContextConverter.DefaultTopicAttribute"/>).</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseSqs(this IMiddlewarePipelineBuilder<OutboundContext> app, string queueUrl, string topicAttributeKey = OutboundSqsContextConverter.DefaultTopicAttribute)
    {
        return app.Convert(new OutboundSqsContextConverter(queueUrl, topicAttributeKey), builder => builder.UseSqsClient());
    }

    /// <summary>
    /// Registers a scoped <see cref="SqsBenzeneMessageClient"/> built from a custom middleware pipeline
    /// configuration.
    /// </summary>
    /// <param name="services">The service container to register on.</param>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <param name="action">A callback used to configure the SQS send pipeline.</param>
    /// <param name="topicAttributeKey">The message attribute the topic is written to (defaults to <see cref="SqsContextConverter{T}.DefaultTopicAttribute"/>).</param>
    /// <returns>The service container, for chaining.</returns>
    public static IBenzeneServiceContainer AddSqsMessageClient(this IBenzeneServiceContainer services, string queueUrl, Action<IMiddlewarePipelineBuilder<SqsSendMessageContext>> action, string topicAttributeKey = OutboundSqsContextConverter.DefaultTopicAttribute)
    {
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<SqsSendMessageContext>(services);
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();

        services.AddScoped(x => new SqsBenzeneMessageClient(queueUrl, pipeline,
            x.GetService<ILogger<SqsBenzeneMessageClient>>(), x.GetService<IServiceResolver>(), topicAttributeKey));
        return services;
    }

    /// <summary>
    /// Adds a health check that pings an SQS queue.
    /// </summary>
    /// <param name="builder">The health check builder to add the check to.</param>
    /// <param name="queueUrl">The URL of the queue to ping.</param>
    /// <param name="topicAttributeKey">
    /// The message attribute the ping topic is written to (defaults to
    /// <see cref="OutboundSqsContextConverter.DefaultTopicAttribute"/>) — pass the same key the queue's
    /// consumer routes on.
    /// </param>
    /// <returns>The health check builder for method chaining.</returns>
    public static IHealthCheckBuilder AddSqsHealthCheck(this IHealthCheckBuilder builder, string queueUrl, string topicAttributeKey = OutboundSqsContextConverter.DefaultTopicAttribute)
    {
        return builder.AddHealthCheck(resolver => new SqsHealthCheck(queueUrl, resolver.GetService<IAmazonSQS>(), topicAttributeKey));
    }
}
