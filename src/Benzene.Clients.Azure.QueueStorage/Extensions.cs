using System;
using Azure.Storage.Queues;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.HealthChecks.Core;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.QueueStorage;

/// <summary>
/// Provides extension methods for wiring <see cref="QueueStorageClientMiddleware"/> into middleware pipelines.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds a <see cref="QueueStorageClientMiddleware"/> built from the given queue client to the pipeline.
    /// </summary>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="queueClient">The queue client used to send messages.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<QueueStorageSendMessageContext> UseQueueStorageClient(
        this IMiddlewarePipelineBuilder<QueueStorageSendMessageContext> app, QueueClient queueClient)
    {
        return app.Use(_ => new QueueStorageClientMiddleware(queueClient));
    }

    /// <summary>
    /// Adds a <see cref="QueueStorageClientMiddleware"/> resolved from the service container to the
    /// pipeline. Requires a <see cref="QueueClient"/> to be registered in the container.
    /// </summary>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<QueueStorageSendMessageContext> UseQueueStorageClient(
        this IMiddlewarePipelineBuilder<QueueStorageSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<QueueStorageClientMiddleware>());
        return app.Use<QueueStorageSendMessageContext, QueueStorageClientMiddleware>();
    }

    /// <summary>
    /// Converts the pipeline to send via Queue Storage (enveloped), using a custom middleware configuration.
    /// </summary>
    /// <typeparam name="T">The type of the outgoing message.</typeparam>
    /// <param name="app">The pipeline builder to convert.</param>
    /// <param name="action">A callback used to configure the converted Queue Storage send pipeline.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseQueueStorage<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app,
        Action<IMiddlewarePipelineBuilder<QueueStorageSendMessageContext>> action)
    {
        return app.Convert(new QueueStorageContextConverter<T>(), action);
    }

    /// <summary>
    /// Converts the pipeline to send via Queue Storage (enveloped), using the default
    /// <see cref="QueueStorageClientMiddleware"/> configuration built from the given queue client.
    /// </summary>
    /// <typeparam name="T">The type of the outgoing message.</typeparam>
    /// <param name="app">The pipeline builder to convert.</param>
    /// <param name="queueClient">The queue client used to send messages.</param>
    /// <param name="healthCheck">
    /// When <c>true</c> (the default) a non-destructive Queue Storage reachability check for
    /// <paramref name="queueClient"/>'s queue is auto-registered on the deep <c>healthcheck</c> layer
    /// (never a Kubernetes probe — see <see cref="IDependencyHealthCheck"/>). Pass <c>false</c> to opt out.
    /// Reuses the given <paramref name="queueClient"/> instance directly.
    /// </param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseQueueStorage<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app, QueueClient queueClient, bool healthCheck = true)
    {
        if (healthCheck)
        {
            app.RegisterQueueStorageDependencyHealthCheck(queueClient);
        }
        return app.Convert(new QueueStorageContextConverter<T>(), builder => builder.UseQueueStorageClient(queueClient));
    }

    /// <summary>
    /// Converts an outbound route pipeline (<c>OutboundRoutingBuilder.Route</c>) to send via Queue Storage
    /// (enveloped), using a custom middleware configuration.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to convert.</param>
    /// <param name="action">A callback used to configure the converted Queue Storage send pipeline.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseQueueStorage(this IMiddlewarePipelineBuilder<OutboundContext> app,
        Action<IMiddlewarePipelineBuilder<QueueStorageSendMessageContext>> action)
    {
        return app.Convert(new OutboundQueueStorageContextConverter(), action);
    }

    /// <summary>
    /// Converts an outbound route pipeline (<c>OutboundRoutingBuilder.Route</c>) to send via Queue Storage
    /// (enveloped), using the default <see cref="QueueStorageClientMiddleware"/> configuration built from
    /// the given queue client.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to convert.</param>
    /// <param name="queueClient">The queue client used to send messages.</param>
    /// <param name="healthCheck">
    /// When <c>true</c> (the default) a non-destructive Queue Storage reachability check for
    /// <paramref name="queueClient"/>'s queue is auto-registered on the deep <c>healthcheck</c> layer
    /// (never a Kubernetes probe). Pass <c>false</c> to opt out.
    /// </param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseQueueStorage(this IMiddlewarePipelineBuilder<OutboundContext> app, QueueClient queueClient, bool healthCheck = true)
    {
        if (healthCheck)
        {
            app.RegisterQueueStorageDependencyHealthCheck(queueClient);
        }
        return app.Convert(new OutboundQueueStorageContextConverter(), builder => builder.UseQueueStorageClient(queueClient));
    }

    /// <summary>
    /// Registers a scoped <see cref="QueueStorageBenzeneMessageClient"/> built from a custom middleware
    /// pipeline configuration.
    /// </summary>
    /// <param name="services">The service container to register on.</param>
    /// <param name="action">A callback used to configure the Queue Storage send pipeline.</param>
    /// <returns>The service container, for chaining.</returns>
    public static IBenzeneServiceContainer AddQueueStorageMessageClient(this IBenzeneServiceContainer services, Action<IMiddlewarePipelineBuilder<QueueStorageSendMessageContext>> action)
    {
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<QueueStorageSendMessageContext>(services);
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();

        services.AddScoped(x => new QueueStorageBenzeneMessageClient(pipeline,
            x.GetService<ILogger<QueueStorageBenzeneMessageClient>>(), x.GetService<IServiceResolver>()));
        return services;
    }

    /// <summary>
    /// Registers a <see cref="QueueStorageHealthCheck"/> for <paramref name="queueClient"/>'s queue.
    /// </summary>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="queueClient">The queue client whose queue to check.</param>
    /// <returns>The health check builder for method chaining.</returns>
    public static IHealthCheckBuilder AddQueueStorageHealthCheck(this IHealthCheckBuilder builder, QueueClient queueClient)
    {
        return builder.AddHealthCheck(_ => new QueueStorageHealthCheck(queueClient));
    }

    // Auto-registers a non-destructive Queue Storage reachability check on the DEPENDENCY category (deep
    // healthcheck layer only, never a k8s probe - shared-fate). Deduped by (Type, Name) via the queue's
    // name so two `.UseQueueStorage(sameQueue)` calls yield one check. Captures the caller's QueueClient
    // instance directly (no DI round-trip - Queue Storage clients are passed, not resolved).
    private static void RegisterQueueStorageDependencyHealthCheck<TContext>(this IMiddlewarePipelineBuilder<TContext> app, QueueClient queueClient)
    {
        app.Register(x => x.AddDependencyHealthCheck(
            _ => new QueueStorageHealthCheck(queueClient),
            $"QueueStorage:{queueClient.Name}"));
    }
}
