using Azure.Messaging.EventHubs;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.Core;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;

namespace Benzene.Azure.Function.EventHub.Function;

/// <summary>
/// Provides extension methods for adding direct-message handling to an Event Hub middleware pipeline,
/// and for dispatching Event Hub events to a built <see cref="IAzureFunctionApp"/>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds direct Benzene message handling to the pipeline, configuring the inner message pipeline inline.
    /// </summary>
    /// <param name="app">The Event Hub pipeline builder to add message handling to.</param>
    /// <param name="action">The action that configures the inner direct-message pipeline.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<EventHubContext> UseBenzeneMessage(this IMiddlewarePipelineBuilder<EventHubContext> app, Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>> action)
    {
        app.Register(x => x.AddBenzeneMessage());
        var middlewarePipelineBuilder = app.Create<BenzeneMessageContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        return app.Use(resolver => new BenzeneMessageEventHubHandler(pipeline, resolver));
    }

    /// <summary>
    /// Adds direct Benzene message handling to the pipeline, using an already-built inner message pipeline.
    /// </summary>
    /// <param name="app">The Event Hub pipeline builder to add message handling to.</param>
    /// <param name="builder">The already-configured inner direct-message pipeline builder.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<EventHubContext> UseBenzeneMessage(this IMiddlewarePipelineBuilder<EventHubContext> app, IMiddlewarePipelineBuilder<BenzeneMessageContext> builder)
    {
        var pipeline = builder.Build();
        return app.Use(resolver => new BenzeneMessageEventHubHandler(pipeline, resolver));
    }

    /// <summary>
    /// Dispatches Event Hub event data to the Azure Function app's Event Hub entry point application.
    /// </summary>
    /// <param name="source">The built Azure Function app to dispatch to.</param>
    /// <param name="eventData">The Event Hub events to handle.</param>
    /// <returns>A task that completes when the batch has been handled.</returns>
    public static Task HandleEventHub(this IAzureFunctionApp source, params EventData[] eventData)
    {
        return source.HandleAsync(eventData);
    }

}
