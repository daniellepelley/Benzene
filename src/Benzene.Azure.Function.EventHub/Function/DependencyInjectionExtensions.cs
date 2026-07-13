using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.Core;

namespace Benzene.Azure.Function.EventHub.Function;

/// <summary>
/// Provides extension methods for adding Event Hub trigger handling to an <see cref="IAzureFunctionAppBuilder"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds an Event Hub entry point application to the Azure Function app, configuring its inner
    /// middleware pipeline.
    /// </summary>
    /// <param name="app">The Azure Function app builder to add Event Hub handling to.</param>
    /// <param name="action">The action that configures the Event Hub middleware pipeline.</param>
    /// <returns>The Azure Function app builder, for method chaining.</returns>
    public static IAzureFunctionAppBuilder UseEventHub(this IAzureFunctionAppBuilder app, Action<IMiddlewarePipelineBuilder<EventHubContext>> action)
    {
        var pipeline = app.Create<EventHubContext>();
        action(pipeline);
        app.Add(serviceResolverFactory => new EventHubApplication(pipeline.Build(), serviceResolverFactory));
        return app;
    }

    /// <summary>
    /// Applies Event Hub-specific configuration to a platform-neutral <see cref="IBenzeneApplicationBuilder"/>.
    /// No-op on any platform other than Azure Functions.
    /// </summary>
    /// <param name="app">The application builder passed to <c>BenzeneStartUp.Configure</c>.</param>
    /// <param name="action">The action that configures the Event Hub middleware pipeline.</param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IBenzeneApplicationBuilder UseEventHub(this IBenzeneApplicationBuilder app, Action<IMiddlewarePipelineBuilder<EventHubContext>> action)
    {
        if (app is IAzureFunctionAppBuilder azureApp)
        {
            azureApp.UseEventHub(action);
        }
        return app;
    }
}
