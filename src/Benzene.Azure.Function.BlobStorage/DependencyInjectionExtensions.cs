using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.Core;
using Benzene.Core.MessageHandlers.Info;

namespace Benzene.Azure.Function.BlobStorage;

/// <summary>
/// Provides extension methods for adding blob trigger handling to an <see cref="IAzureFunctionAppBuilder"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services blob-trigger handling depends on beyond the entry point application
    /// itself - currently just the <see cref="ITransportInfo"/> advertising <c>"blob-storage"</c>
    /// as a wired transport. Called automatically by <see cref="UseBlobStorage"/>.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddAzureBlobStorage(this IBenzeneServiceContainer services)
    {
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.BlobStorage));
        return services;
    }

    /// <summary>
    /// Adds a blob entry point application to the Azure Function app, configuring its inner
    /// middleware pipeline. There is no <c>UseMessageHandlers()</c>-style routing on this transport
    /// - a blob is a file, not a message envelope, and one blob-trigger function watches one
    /// container path - so the pipeline is consumed with <c>UseBlob(...)</c> (or any ordinary
    /// middleware), composing with correlation/metrics/exception middleware as usual.
    /// </summary>
    /// <param name="app">The Azure Function app builder to add blob handling to.</param>
    /// <param name="action">The action that configures the blob middleware pipeline.</param>
    /// <returns>The Azure Function app builder, for method chaining.</returns>
    public static IAzureFunctionAppBuilder UseBlobStorage(this IAzureFunctionAppBuilder app, Action<IMiddlewarePipelineBuilder<BlobStorageContext>> action)
    {
        app.Register(x => x.AddAzureBlobStorage());
        var pipeline = app.Create<BlobStorageContext>();
        action(pipeline);
        app.Add(serviceResolverFactory => new BlobStorageApplication(pipeline.Build(), serviceResolverFactory));
        return app;
    }

    /// <summary>
    /// Applies blob-trigger configuration to a platform-neutral <see cref="IBenzeneApplicationBuilder"/>.
    /// No-op on any platform other than Azure Functions.
    /// </summary>
    /// <param name="app">The application builder passed to <c>BenzeneStartUp.Configure</c>.</param>
    /// <param name="action">The action that configures the blob middleware pipeline.</param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IBenzeneApplicationBuilder UseBlobStorage(this IBenzeneApplicationBuilder app, Action<IMiddlewarePipelineBuilder<BlobStorageContext>> action)
    {
        if (app is IAzureFunctionAppBuilder azureApp)
        {
            azureApp.UseBlobStorage(action);
        }
        return app;
    }
}
