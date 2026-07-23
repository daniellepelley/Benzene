using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.DI;
using Benzene.SelfHost;

namespace Benzene.Azure.EventHub;

/// <summary>
/// Provides extension methods for adding a standalone Event Hub consumer to a Benzene worker.
/// </summary>
/// <remarks>
/// Unlike <c>Benzene.Azure.Function.EventHub</c>, which processes events delivered via an Azure
/// Functions Event Hub trigger, this package consumes a hub directly using
/// <see cref="BenzeneEventHubWorker"/> - intended for long-running workers
/// (e.g. <c>Benzene.HostedService</c>/<c>Benzene.SelfHost</c>) rather than Azure Functions.
/// </remarks>
public static class Extensions
{
    /// <summary>
    /// Adds an Event Hub consumer to the worker.
    /// </summary>
    /// <param name="app">The worker startup to add the Event Hub consumer to.</param>
    /// <param name="config">The checkpointing and failure-handling behavior to use.</param>
    /// <param name="eventProcessorClientFactory">
    /// The factory used to create the underlying <c>EventProcessorClient</c> (which decides the
    /// hub, consumer group, blob checkpoint container, and authentication).
    /// </param>
    /// <param name="action">The action that configures the inner Event Hub message pipeline.</param>
    /// <returns>The worker startup for method chaining.</returns>
    public static IBenzeneWorkerStartup UseEventHub(this IBenzeneWorkerStartup app, BenzeneEventHubConfig config, IEventProcessorClientFactory eventProcessorClientFactory, Action<IMiddlewarePipelineBuilder<EventHubConsumerContext>> action)
    {
        app.Register(x => x
            .AddBenzeneMessage()
            .AddEventHubConsumer(config.TopicPropertyKey)
        );
        var middlewarePipelineBuilder = app.Create<EventHubConsumerContext>();
        middlewarePipelineBuilder.UseBenzeneInvocation();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();

        var application = new EventHubConsumerApplication(pipeline);
        // Register the built application so it can be resolved and driven directly - e.g. a
        // StartUp-based component test pushing an event through the real pipeline without a running
        // hub (see Benzene.Azure.EventHub.TestHelpers). Inert in a normal worker run; the worker
        // already holds this same instance via the factory below.
        app.Register(x => x.AddSingleton(application));
        app.Add(serviceResolverFactory => new BenzeneEventHubWorker(serviceResolverFactory, application, config, eventProcessorClientFactory));
        return app;
    }
}
