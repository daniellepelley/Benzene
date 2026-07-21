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
    /// <remarks>
    /// Preserved for backward compatibility. To also configure exception isolation
    /// (<see cref="EventHubOptions.CatchExceptions"/>) or failure-result escalation
    /// (<see cref="EventHubOptions.RaiseOnFailureStatus"/>), use the
    /// <see cref="UseEventHub(IAzureFunctionAppBuilder, Action{IMiddlewarePipelineBuilder{EventHubContext}}, Action{EventHubOptions})"/>
    /// overload instead.
    /// </remarks>
    public static IAzureFunctionAppBuilder UseEventHub(this IAzureFunctionAppBuilder app, Action<IMiddlewarePipelineBuilder<EventHubContext>> action, int? maxDegreeOfParallelism = null)
    {
        return app.UseEventHub(action, options => options.MaxDegreeOfParallelism = maxDegreeOfParallelism);
    }

    /// <summary>
    /// Adds an Event Hub entry point application to the Azure Function app, configuring its inner
    /// middleware pipeline and its exception/failure-status/fan-out behavior via <see cref="EventHubOptions"/>.
    /// </summary>
    /// <param name="app">The Azure Function app builder to add Event Hub handling to.</param>
    /// <param name="action">The action that configures the Event Hub middleware pipeline.</param>
    /// <param name="configure">
    /// Configures the <see cref="EventHubOptions"/> - exception isolation, failure-result escalation,
    /// and fan-out concurrency. Defaults (both flags off, unbounded) preserve the original behavior.
    /// </param>
    /// <returns>The Azure Function app builder, for method chaining.</returns>
    public static IAzureFunctionAppBuilder UseEventHub(this IAzureFunctionAppBuilder app, Action<IMiddlewarePipelineBuilder<EventHubContext>> action, Action<EventHubOptions> configure)
    {
        app.Register(x => x.AddAzureEventHub());
        var pipeline = app.Create<EventHubContext>();
        pipeline.UseBenzeneInvocation();
        action(pipeline);
        var options = new EventHubOptions();
        configure?.Invoke(options);
        app.Add(serviceResolverFactory => new EventHubApplication(pipeline.Build(), serviceResolverFactory, options));
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

    /// <summary>
    /// Applies Event Hub-specific configuration - including <see cref="EventHubOptions"/> - to a
    /// platform-neutral <see cref="IBenzeneApplicationBuilder"/>. No-op on any platform other than
    /// Azure Functions.
    /// </summary>
    /// <param name="app">The application builder passed to <c>BenzeneStartUp.Configure</c>.</param>
    /// <param name="action">The action that configures the Event Hub middleware pipeline.</param>
    /// <param name="configure">Configures the <see cref="EventHubOptions"/> for the entry point.</param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IBenzeneApplicationBuilder UseEventHub(this IBenzeneApplicationBuilder app, Action<IMiddlewarePipelineBuilder<EventHubContext>> action, Action<EventHubOptions> configure)
    {
        if (app is IAzureFunctionAppBuilder azureApp)
        {
            azureApp.UseEventHub(action, configure);
        }
        return app;
    }
}
