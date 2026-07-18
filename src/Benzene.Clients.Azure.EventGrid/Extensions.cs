using System;
using Azure.Messaging.EventGrid;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.EventGrid;

/// <summary>
/// Provides extension methods for wiring <see cref="EventGridClientMiddleware"/> into middleware pipelines.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds an <see cref="EventGridClientMiddleware"/> built from the given publisher client to the pipeline.
    /// </summary>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="publisherClient">The Event Grid publisher client used to send events.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<EventGridSendMessageContext> UseEventGridClient(
        this IMiddlewarePipelineBuilder<EventGridSendMessageContext> app, EventGridPublisherClient publisherClient)
    {
        return app.Use(_ => new EventGridClientMiddleware(publisherClient));
    }

    /// <summary>
    /// Adds an <see cref="EventGridClientMiddleware"/> resolved from the service container to the pipeline.
    /// Requires an <see cref="EventGridPublisherClient"/> to be registered in the container.
    /// </summary>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<EventGridSendMessageContext> UseEventGridClient(
        this IMiddlewarePipelineBuilder<EventGridSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<EventGridClientMiddleware>());
        return app.Use<EventGridSendMessageContext, EventGridClientMiddleware>();
    }

    /// <summary>
    /// Converts the pipeline to send via Event Grid as CloudEvents 1.0, using a custom middleware
    /// configuration.
    /// </summary>
    /// <typeparam name="T">The type of the outgoing message.</typeparam>
    /// <param name="app">The pipeline builder to convert.</param>
    /// <param name="source">The CloudEvent <c>source</c> - identifies the context the event happened in.</param>
    /// <param name="action">A callback used to configure the converted Event Grid send pipeline.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseEventGrid<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app,
        string source,
        Action<IMiddlewarePipelineBuilder<EventGridSendMessageContext>> action)
    {
        return app.Convert(new EventGridContextConverter<T>(source), action);
    }

    /// <summary>
    /// Converts the pipeline to send via Event Grid as CloudEvents 1.0, using the default
    /// <see cref="EventGridClientMiddleware"/> configuration built from the given publisher client.
    /// </summary>
    /// <typeparam name="T">The type of the outgoing message.</typeparam>
    /// <param name="app">The pipeline builder to convert.</param>
    /// <param name="source">The CloudEvent <c>source</c> - identifies the context the event happened in.</param>
    /// <param name="publisherClient">The Event Grid publisher client used to send events.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseEventGrid<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app, string source, EventGridPublisherClient publisherClient)
    {
        return app.Convert(new EventGridContextConverter<T>(source), builder => builder.UseEventGridClient(publisherClient));
    }

    /// <summary>
    /// Converts an outbound route pipeline (<c>OutboundRoutingBuilder.Route</c>) to send via Event Grid as
    /// CloudEvents 1.0, using a custom middleware configuration.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to convert.</param>
    /// <param name="source">The CloudEvent <c>source</c> - identifies the context the event happened in.</param>
    /// <param name="action">A callback used to configure the converted Event Grid send pipeline.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseEventGrid(this IMiddlewarePipelineBuilder<OutboundContext> app,
        string source,
        Action<IMiddlewarePipelineBuilder<EventGridSendMessageContext>> action)
    {
        return app.Convert(new OutboundEventGridContextConverter(source), action);
    }

    /// <summary>
    /// Converts an outbound route pipeline (<c>OutboundRoutingBuilder.Route</c>) to send via Event Grid as
    /// CloudEvents 1.0, using the default <see cref="EventGridClientMiddleware"/> configuration built from
    /// the given publisher client.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to convert.</param>
    /// <param name="source">The CloudEvent <c>source</c> - identifies the context the event happened in.</param>
    /// <param name="publisherClient">The Event Grid publisher client used to send events.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseEventGrid(this IMiddlewarePipelineBuilder<OutboundContext> app, string source, EventGridPublisherClient publisherClient)
    {
        return app.Convert(new OutboundEventGridContextConverter(source), builder => builder.UseEventGridClient(publisherClient));
    }

    /// <summary>
    /// Converts the pipeline to send via Event Grid using the classic Event Grid schema (not CloudEvents),
    /// using a custom middleware configuration. Prefer <see cref="UseEventGrid{T}(IMiddlewarePipelineBuilder{IBenzeneClientContext{T,Void}},string,Action{IMiddlewarePipelineBuilder{EventGridSendMessageContext}})"/>
    /// for new code.
    /// </summary>
    /// <typeparam name="T">The type of the outgoing message.</typeparam>
    /// <param name="app">The pipeline builder to convert.</param>
    /// <param name="action">A callback used to configure the converted Event Grid send pipeline.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseEventGridEventSchema<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app,
        Action<IMiddlewarePipelineBuilder<EventGridSendMessageContext>> action)
    {
        return app.Convert(new EventGridEventSchemaContextConverter<T>(), action);
    }

    /// <summary>
    /// Converts an outbound route pipeline (<c>OutboundRoutingBuilder.Route</c>) to send via Event Grid
    /// using the classic Event Grid schema (not CloudEvents), using a custom middleware configuration.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to convert.</param>
    /// <param name="action">A callback used to configure the converted Event Grid send pipeline.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseEventGridEventSchema(this IMiddlewarePipelineBuilder<OutboundContext> app,
        Action<IMiddlewarePipelineBuilder<EventGridSendMessageContext>> action)
    {
        return app.Convert(new OutboundEventGridEventSchemaContextConverter(), action);
    }

    /// <summary>
    /// Registers a scoped <see cref="EventGridBenzeneMessageClient"/> built from a custom middleware
    /// pipeline configuration.
    /// </summary>
    /// <param name="services">The service container to register on.</param>
    /// <param name="source">The CloudEvent <c>source</c> - identifies the context the event happened in.</param>
    /// <param name="action">A callback used to configure the Event Grid send pipeline.</param>
    /// <returns>The service container, for chaining.</returns>
    public static IBenzeneServiceContainer AddEventGridMessageClient(this IBenzeneServiceContainer services, string source, Action<IMiddlewarePipelineBuilder<EventGridSendMessageContext>> action)
    {
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<EventGridSendMessageContext>(services);
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();

        services.AddScoped(x => new EventGridBenzeneMessageClient(source, pipeline,
            x.GetService<ILogger<EventGridBenzeneMessageClient>>(), x.GetService<IServiceResolver>()));
        return services;
    }
}
