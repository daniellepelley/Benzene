using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.DI;
using Benzene.SelfHost;

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// Provides extension methods for adding a standalone Service Bus consumer to a Benzene worker.
/// </summary>
/// <remarks>
/// Unlike <c>Benzene.Azure.Function.ServiceBus</c>, which processes messages delivered via an Azure
/// Functions Service Bus trigger, this package consumes an entity directly using
/// <see cref="BenzeneServiceBusWorker"/> - intended for long-running workers
/// (e.g. <c>Benzene.HostedService</c>/<c>Benzene.SelfHost</c>) rather than Azure Functions.
/// </remarks>
public static class Extensions
{
    /// <summary>
    /// Adds a Service Bus consumer to the worker.
    /// </summary>
    /// <param name="app">The worker startup to add the Service Bus consumer to.</param>
    /// <param name="config">The entity to consume and the processing behavior to use.</param>
    /// <param name="serviceBusClientFactory">The factory used to create the underlying <c>ServiceBusClient</c>.</param>
    /// <param name="action">The action that configures the inner Service Bus message pipeline.</param>
    /// <param name="healthCheck">
    /// When <c>true</c> (the default) a non-destructive Service Bus reachability check (a peek of the
    /// consumed entity, using the <c>Listen</c> claim the consumer holds) is auto-registered on the deep
    /// <c>healthcheck</c> layer — never a Kubernetes probe (a broker being unreachable is shared-fate; see
    /// <c>IDependencyHealthCheck</c>). Pass <c>false</c> to opt out.
    /// </param>
    /// <returns>The worker startup for method chaining.</returns>
    public static IBenzeneWorkerStartup UseServiceBus(this IBenzeneWorkerStartup app, BenzeneServiceBusConfig config, IServiceBusClientFactory serviceBusClientFactory, Action<IMiddlewarePipelineBuilder<ServiceBusConsumerContext>> action,
        bool healthCheck = true)
    {
        app.Register(x => x
            .AddBenzeneMessage()
            .AddServiceBusConsumer(config.TopicPropertyKey)
        );

        if (healthCheck)
        {
            app.Register(x => x.AddServiceBusDependencyHealthCheck(config, serviceBusClientFactory));
        }
        var middlewarePipelineBuilder = app.Create<ServiceBusConsumerContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();

        var application = new ServiceBusConsumerApplication(pipeline);
        app.Add(serviceResolverFactory => new BenzeneServiceBusWorker(serviceResolverFactory, application, config, serviceBusClientFactory));
        return app;
    }
}
