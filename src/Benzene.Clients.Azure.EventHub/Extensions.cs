using System;
using Azure.Messaging.EventHubs.Producer;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.HealthChecks.Core;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.EventHub;

/// <summary>
/// Provides extension methods for wiring <see cref="EventHubClientMiddleware"/> into middleware pipelines.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds an <see cref="EventHubClientMiddleware"/> built from the given producer client to the pipeline.
    /// </summary>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="producerClient">The Event Hubs producer client used to send events.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<EventHubSendMessageContext> UseEventHubClient(
        this IMiddlewarePipelineBuilder<EventHubSendMessageContext> app, EventHubProducerClient producerClient)
    {
        return app.Use(_ => new EventHubClientMiddleware(producerClient));
    }

    /// <summary>
    /// Adds an <see cref="EventHubClientMiddleware"/> resolved from the service container to the pipeline.
    /// Requires an <see cref="EventHubProducerClient"/> to be registered in the container.
    /// </summary>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<EventHubSendMessageContext> UseEventHubClient(
        this IMiddlewarePipelineBuilder<EventHubSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<EventHubClientMiddleware>());
        return app.Use<EventHubSendMessageContext, EventHubClientMiddleware>();
    }

    /// <summary>
    /// Converts the pipeline to send via Event Hubs, using a custom middleware configuration.
    /// </summary>
    /// <typeparam name="T">The type of the outgoing message.</typeparam>
    /// <param name="app">The pipeline builder to convert.</param>
    /// <param name="action">A callback used to configure the converted Event Hubs send pipeline.</param>
    /// <param name="topicPropertyKey">The event property the topic is written to (defaults to <see cref="EventHubContextConverter{T}.DefaultTopicProperty"/>).</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseEventHub<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app,
        Action<IMiddlewarePipelineBuilder<EventHubSendMessageContext>> action,
        string topicPropertyKey = EventHubContextConverter<T>.DefaultTopicProperty,
        string partitionKeyHeader = null)
    {
        return app.Convert(new EventHubContextConverter<T>(topicPropertyKey, partitionKeyHeader), action);
    }

    /// <summary>
    /// Converts the pipeline to send via Event Hubs, using the default <see cref="EventHubClientMiddleware"/>
    /// configuration built from the given producer client.
    /// </summary>
    /// <typeparam name="T">The type of the outgoing message.</typeparam>
    /// <param name="app">The pipeline builder to convert.</param>
    /// <param name="producerClient">The Event Hubs producer client used to send events.</param>
    /// <param name="topicPropertyKey">The event property the topic is written to (defaults to <see cref="EventHubContextConverter{T}.DefaultTopicProperty"/>).</param>
    /// <param name="partitionKeyHeader">The request header whose value becomes the batch partition key (null = no key).</param>
    /// <param name="healthCheck">
    /// When <c>true</c> (the default) a non-destructive Event Hubs reachability check for
    /// <paramref name="producerClient"/>'s hub is auto-registered on the deep <c>healthcheck</c> layer
    /// (never a Kubernetes probe — see <see cref="IDependencyHealthCheck"/>). Pass <c>false</c> to opt out.
    /// Reuses the given <paramref name="producerClient"/> instance directly.
    /// </param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseEventHub<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app, EventHubProducerClient producerClient, string topicPropertyKey = EventHubContextConverter<T>.DefaultTopicProperty, string partitionKeyHeader = null, bool healthCheck = true)
    {
        if (healthCheck)
        {
            app.RegisterEventHubDependencyHealthCheck(producerClient);
        }
        return app.Convert(new EventHubContextConverter<T>(topicPropertyKey, partitionKeyHeader), builder => builder.UseEventHubClient(producerClient));
    }

    /// <summary>
    /// Converts an outbound route pipeline (<c>OutboundRoutingBuilder.Route</c>) to send via Event Hubs,
    /// using a custom middleware configuration.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to convert.</param>
    /// <param name="action">A callback used to configure the converted Event Hubs send pipeline.</param>
    /// <param name="topicPropertyKey">The event property the topic is written to (defaults to <see cref="OutboundEventHubContextConverter.DefaultTopicProperty"/>).</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseEventHub(this IMiddlewarePipelineBuilder<OutboundContext> app,
        Action<IMiddlewarePipelineBuilder<EventHubSendMessageContext>> action,
        string topicPropertyKey = OutboundEventHubContextConverter.DefaultTopicProperty,
        string partitionKeyHeader = null)
    {
        return app.Convert(new OutboundEventHubContextConverter(topicPropertyKey, partitionKeyHeader), action);
    }

    /// <summary>
    /// Converts an outbound route pipeline (<c>OutboundRoutingBuilder.Route</c>) to send via Event Hubs,
    /// using the default <see cref="EventHubClientMiddleware"/> configuration built from the given
    /// producer client.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to convert.</param>
    /// <param name="producerClient">The Event Hubs producer client used to send events.</param>
    /// <param name="topicPropertyKey">The event property the topic is written to (defaults to <see cref="OutboundEventHubContextConverter.DefaultTopicProperty"/>).</param>
    /// <param name="healthCheck">
    /// When <c>true</c> (the default) a non-destructive Event Hubs reachability check for
    /// <paramref name="producerClient"/>'s hub is auto-registered on the deep <c>healthcheck</c> layer
    /// (never a Kubernetes probe). Pass <c>false</c> to opt out.
    /// </param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseEventHub(this IMiddlewarePipelineBuilder<OutboundContext> app, EventHubProducerClient producerClient, string topicPropertyKey = OutboundEventHubContextConverter.DefaultTopicProperty, bool healthCheck = true)
    {
        if (healthCheck)
        {
            app.RegisterEventHubDependencyHealthCheck(producerClient);
        }
        return app.Convert(new OutboundEventHubContextConverter(topicPropertyKey), builder => builder.UseEventHubClient(producerClient));
    }

    /// <summary>
    /// Registers a scoped <see cref="EventHubBenzeneMessageClient"/> built from a custom middleware
    /// pipeline configuration.
    /// </summary>
    /// <param name="services">The service container to register on.</param>
    /// <param name="action">A callback used to configure the Event Hubs send pipeline.</param>
    /// <param name="topicPropertyKey">The event property the topic is written to (defaults to <see cref="EventHubContextConverter{T}.DefaultTopicProperty"/>).</param>
    /// <returns>The service container, for chaining.</returns>
    public static IBenzeneServiceContainer AddEventHubMessageClient(this IBenzeneServiceContainer services, Action<IMiddlewarePipelineBuilder<EventHubSendMessageContext>> action, string topicPropertyKey = OutboundEventHubContextConverter.DefaultTopicProperty)
    {
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<EventHubSendMessageContext>(services);
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();

        services.AddScoped(x => new EventHubBenzeneMessageClient(pipeline,
            x.GetService<ILogger<EventHubBenzeneMessageClient>>(), x.GetService<IServiceResolver>(), topicPropertyKey));
        return services;
    }

    /// <summary>
    /// Registers an <see cref="EventHubHealthCheck"/> for <paramref name="producerClient"/>'s hub.
    /// </summary>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="producerClient">The producer client whose hub to check.</param>
    /// <returns>The health check builder for method chaining.</returns>
    public static IHealthCheckBuilder AddEventHubHealthCheck(this IHealthCheckBuilder builder, EventHubProducerClient producerClient)
    {
        return builder.AddHealthCheck(_ => new EventHubHealthCheck(producerClient, producerClient.EventHubName));
    }

    // Auto-registers a non-destructive Event Hubs reachability check on the DEPENDENCY category (deep
    // healthcheck layer only, never a k8s probe - shared-fate). Deduped by (Type, Name) via the hub name.
    // Captures the caller's EventHubProducerClient instance directly (no DI round-trip - Event Hubs clients
    // are passed, not resolved).
    private static void RegisterEventHubDependencyHealthCheck<TContext>(this IMiddlewarePipelineBuilder<TContext> app, EventHubProducerClient producerClient)
    {
        app.Register(x => x.AddDependencyHealthCheck(
            _ => new EventHubHealthCheck(producerClient, producerClient.EventHubName),
            $"EventHub:{producerClient.EventHubName}"));
    }
}
