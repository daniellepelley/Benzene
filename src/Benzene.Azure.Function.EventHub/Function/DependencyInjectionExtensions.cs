using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.Core;
using Benzene.Core.MessageHandlers.Info;

namespace Benzene.Azure.Function.EventHub.Function;

/// <summary>
/// Provides extension methods for adding Event Hub trigger handling to an <see cref="IAzureFunctionAppBuilder"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to process Event Hub-triggered events.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container, for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="UseEventHub(IAzureFunctionAppBuilder, Action{IMiddlewarePipelineBuilder{EventHubContext}})"/>;
    /// you don't normally need to call this directly. This package has no first-class mappers of its
    /// own beyond headers - it routes Benzene message envelopes via <c>UseBenzeneMessage</c> instead
    /// of first-class topic/body mappers on <see cref="EventHubContext"/> itself.
    /// </remarks>
    public static IBenzeneServiceContainer AddAzureEventHub(this IBenzeneServiceContainer services)
    {
        services.AddScoped<IMessageHeadersGetter<EventHubContext>, EventHubMessageHeadersGetter>();
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.EventHub));
        return services;
    }

    /// <summary>
    /// Adds an Event Hub entry point application to the Azure Function app, configuring its inner
    /// middleware pipeline.
    /// </summary>
    /// <param name="app">The Azure Function app builder to add Event Hub handling to.</param>
    /// <param name="action">The action that configures the Event Hub middleware pipeline.</param>
    /// <param name="maxDegreeOfParallelism">
    /// Optionally caps how many events from a batch run at once; <c>null</c> (the default) leaves the
    /// fan-out unbounded - the original behavior.
    /// </param>
    /// <returns>The Azure Function app builder, for method chaining.</returns>
    public static IAzureFunctionAppBuilder UseEventHub(this IAzureFunctionAppBuilder app, Action<IMiddlewarePipelineBuilder<EventHubContext>> action, int? maxDegreeOfParallelism = null)
    {
        app.Register(x => x.AddAzureEventHub());
        var pipeline = app.Create<EventHubContext>();
        pipeline.UseBenzeneInvocation();
        action(pipeline);
        app.Add(serviceResolverFactory => new EventHubApplication(pipeline.Build(), serviceResolverFactory, maxDegreeOfParallelism));
        return app;
    }

    /// <summary>
    /// Applies Event Hub-specific configuration to a platform-neutral <see cref="IBenzeneApplicationBuilder"/>.
    /// No-op on any platform other than Azure Functions.
    /// </summary>
    /// <param name="app">The application builder passed to <c>BenzeneStartUp.Configure</c>.</param>
    /// <param name="action">The action that configures the Event Hub middleware pipeline.</param>
    /// <param name="maxDegreeOfParallelism">
    /// Optionally caps how many events from a batch run at once; <c>null</c> (the default) leaves the
    /// fan-out unbounded - the original behavior.
    /// </param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IBenzeneApplicationBuilder UseEventHub(this IBenzeneApplicationBuilder app, Action<IMiddlewarePipelineBuilder<EventHubContext>> action, int? maxDegreeOfParallelism = null)
    {
        if (app is IAzureFunctionAppBuilder azureApp)
        {
            azureApp.UseEventHub(action, maxDegreeOfParallelism);
        }
        return app;
    }
}
