using System;
using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.ServiceBus;

/// <summary>
/// Provides extension methods for wiring <see cref="ServiceBusClientMiddleware"/> into middleware pipelines.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds a <see cref="ServiceBusClientMiddleware"/> built from the given Service Bus sender to the pipeline.
    /// </summary>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="sender">The Service Bus sender (bound to a queue or topic) used to send messages.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<ServiceBusSendMessageContext> UseServiceBusClient(
        this IMiddlewarePipelineBuilder<ServiceBusSendMessageContext> app, ServiceBusSender sender)
    {
        return app.Use(_ => new ServiceBusClientMiddleware(sender));
    }

    /// <summary>
    /// Adds a <see cref="ServiceBusClientMiddleware"/> resolved from the service container to the pipeline.
    /// Requires a <see cref="ServiceBusSender"/> to be registered in the container.
    /// </summary>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<ServiceBusSendMessageContext> UseServiceBusClient(
        this IMiddlewarePipelineBuilder<ServiceBusSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<ServiceBusClientMiddleware>());
        return app.Use<ServiceBusSendMessageContext, ServiceBusClientMiddleware>();
    }

    /// <summary>
    /// Converts the pipeline to send via Service Bus, using a custom middleware configuration.
    /// </summary>
    /// <typeparam name="T">The type of the outgoing message.</typeparam>
    /// <param name="app">The pipeline builder to convert.</param>
    /// <param name="action">A callback used to configure the converted Service Bus send pipeline.</param>
    /// <param name="topicPropertyKey">The application property the topic is written to (defaults to <see cref="ServiceBusContextConverter{T}.DefaultTopicProperty"/>).</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseServiceBus<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app,
        Action<IMiddlewarePipelineBuilder<ServiceBusSendMessageContext>> action,
        string topicPropertyKey = ServiceBusContextConverter<T>.DefaultTopicProperty)
    {
        return app.Convert(new ServiceBusContextConverter<T>(topicPropertyKey), action);
    }

    /// <summary>
    /// Converts the pipeline to send via Service Bus, using the default
    /// <see cref="ServiceBusClientMiddleware"/> configuration built from the given sender.
    /// </summary>
    /// <typeparam name="T">The type of the outgoing message.</typeparam>
    /// <param name="app">The pipeline builder to convert.</param>
    /// <param name="sender">The Service Bus sender (bound to a queue or topic) used to send messages.</param>
    /// <param name="topicPropertyKey">The application property the topic is written to (defaults to <see cref="ServiceBusContextConverter{T}.DefaultTopicProperty"/>).</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseServiceBus<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app, ServiceBusSender sender, string topicPropertyKey = ServiceBusContextConverter<T>.DefaultTopicProperty)
    {
        return app.Convert(new ServiceBusContextConverter<T>(topicPropertyKey), builder => builder.UseServiceBusClient(sender));
    }

    /// <summary>
    /// Converts an outbound route pipeline (<c>OutboundRoutingBuilder.Route</c>) to send via Service Bus,
    /// using a custom middleware configuration.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to convert.</param>
    /// <param name="action">A callback used to configure the converted Service Bus send pipeline.</param>
    /// <param name="topicPropertyKey">The application property the topic is written to (defaults to <see cref="OutboundServiceBusContextConverter.DefaultTopicProperty"/>).</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseServiceBus(this IMiddlewarePipelineBuilder<OutboundContext> app,
        Action<IMiddlewarePipelineBuilder<ServiceBusSendMessageContext>> action,
        string topicPropertyKey = OutboundServiceBusContextConverter.DefaultTopicProperty)
    {
        return app.Convert(new OutboundServiceBusContextConverter(topicPropertyKey), action);
    }

    /// <summary>
    /// Converts an outbound route pipeline (<c>OutboundRoutingBuilder.Route</c>) to send via Service Bus,
    /// using the default <see cref="ServiceBusClientMiddleware"/> configuration built from the given sender.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to convert.</param>
    /// <param name="sender">The Service Bus sender (bound to a queue or topic) used to send messages.</param>
    /// <param name="topicPropertyKey">The application property the topic is written to (defaults to <see cref="OutboundServiceBusContextConverter.DefaultTopicProperty"/>).</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseServiceBus(this IMiddlewarePipelineBuilder<OutboundContext> app, ServiceBusSender sender, string topicPropertyKey = OutboundServiceBusContextConverter.DefaultTopicProperty)
    {
        return app.Convert(new OutboundServiceBusContextConverter(topicPropertyKey), builder => builder.UseServiceBusClient(sender));
    }

    /// <summary>
    /// Registers a scoped <see cref="ServiceBusBenzeneMessageClient"/> built from a custom middleware
    /// pipeline configuration.
    /// </summary>
    /// <param name="services">The service container to register on.</param>
    /// <param name="action">A callback used to configure the Service Bus send pipeline.</param>
    /// <param name="topicPropertyKey">The application property the topic is written to (defaults to <see cref="ServiceBusContextConverter{T}.DefaultTopicProperty"/>).</param>
    /// <returns>The service container, for chaining.</returns>
    public static IBenzeneServiceContainer AddServiceBusMessageClient(this IBenzeneServiceContainer services, Action<IMiddlewarePipelineBuilder<ServiceBusSendMessageContext>> action, string topicPropertyKey = OutboundServiceBusContextConverter.DefaultTopicProperty)
    {
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<ServiceBusSendMessageContext>(services);
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();

        services.AddScoped(x => new ServiceBusBenzeneMessageClient(pipeline,
            x.GetService<ILogger<ServiceBusBenzeneMessageClient>>(), x.GetService<IServiceResolver>(), topicPropertyKey));
        return services;
    }
}
