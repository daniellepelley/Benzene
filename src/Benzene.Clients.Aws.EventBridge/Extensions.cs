using System;
using Amazon.EventBridge;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.HealthChecks.Core;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.EventBridge;

/// <summary>
/// Provides pipeline-composition extensions for publishing to EventBridge, mirroring the SNS/SQS
/// client building blocks.
/// </summary>
public static class Extensions
{
    public static IMiddlewarePipelineBuilder<EventBridgeSendMessageContext> UseEventBridgeClient(
        this IMiddlewarePipelineBuilder<EventBridgeSendMessageContext> app, IAmazonEventBridge amazonEventBridge)
    {
        return app.Use(_ => new EventBridgeClientMiddleware(amazonEventBridge));
    }

    public static IMiddlewarePipelineBuilder<EventBridgeSendMessageContext> UseEventBridgeClient(
        this IMiddlewarePipelineBuilder<EventBridgeSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<EventBridgeClientMiddleware>());
        return app.Use<EventBridgeSendMessageContext, EventBridgeClientMiddleware>();
    }

    public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
        IContextConverter<TContext, TContextOut> converter, Action<IMiddlewarePipelineBuilder<TContextOut>> action)
    {
        var middlewarePipeline = app.CreateMiddlewarePipeline(action);
        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }

    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseEventBridge<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app,
        string source, Action<IMiddlewarePipelineBuilder<EventBridgeSendMessageContext>> action)
    {
        return Convert(app, new EventBridgeContextConverter<T>(source), action);
    }

    /// <summary>
    /// Converts a Benzene client pipeline to publish to EventBridge (default bus), resolving the client from DI.
    /// </summary>
    /// <typeparam name="T">The message payload type being sent.</typeparam>
    /// <param name="app">The client pipeline builder to add EventBridge publishing to.</param>
    /// <param name="source">The EventBridge event <c>source</c> stamped on published events.</param>
    /// <param name="healthCheck">
    /// When <c>true</c> (the default) a non-destructive EventBridge reachability check for the default bus
    /// is auto-registered on the deep <c>healthcheck</c> layer (never a Kubernetes probe — see
    /// <see cref="IDependencyHealthCheck"/>). Pass <c>false</c> to opt out. Reuses the <c>IAmazonEventBridge</c>
    /// resolved from DI.
    /// </param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseEventBridge<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app, string source, bool healthCheck = true)
    {
        if (healthCheck)
        {
            app.RegisterEventBridgeDependencyHealthCheck(EventBridgeHealthCheck.DefaultEventBusName);
        }
        return app.Convert(new EventBridgeContextConverter<T>(source), builder => builder.UseEventBridgeClient());
    }

    /// <summary>
    /// Registers an <see cref="EventBridgeHealthCheck"/> for <paramref name="eventBusName"/> (null = default
    /// bus), resolving <see cref="IAmazonEventBridge"/> from DI (the consumer must register it).
    /// </summary>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="eventBusName">The event bus to check; null/empty targets the default bus.</param>
    /// <returns>The health check builder for method chaining.</returns>
    public static IHealthCheckBuilder AddEventBridgeHealthCheck(this IHealthCheckBuilder builder, string eventBusName = null)
    {
        return builder.AddHealthCheck(resolver => new EventBridgeHealthCheck(resolver.GetService<IAmazonEventBridge>(), eventBusName));
    }

    // Auto-registers a non-destructive EventBridge reachability check on the DEPENDENCY category (deep
    // healthcheck layer only, never a k8s probe - shared-fate). Deduped by (Type, Name). Reuses the
    // IAmazonEventBridge from DI - the same handle `.UseEventBridgeClient()` resolves.
    private static void RegisterEventBridgeDependencyHealthCheck<TContext>(this IMiddlewarePipelineBuilder<TContext> app, string eventBusName)
    {
        app.Register(x => x.AddDependencyHealthCheck(
            resolver => new EventBridgeHealthCheck(resolver.GetService<IAmazonEventBridge>(), eventBusName),
            $"EventBridge:{eventBusName}"));
    }
}
