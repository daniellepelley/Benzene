using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Azure.Function.QueueStorage;

/// <summary>
/// Provides extension methods for registering Queue Storage message-handling services and adding
/// Queue Storage trigger handling to an <see cref="IAzureFunctionAppBuilder"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to process Queue Storage-triggered messages.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container, for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="UseQueueStorage(IAzureFunctionAppBuilder, Action{IMiddlewarePipelineBuilder{QueueStorageContext}})"/>; you don't normally need to call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddAzureQueueStorage(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();
        services.TryAddScoped<PresetTopicHolder>();

        services.AddScoped<IMessageTopicGetter<QueueStorageContext>>(resolver =>
            new PresetTopicMessageTopicGetter<QueueStorageContext>(new QueueStorageMessageTopicGetter(), resolver.GetService<PresetTopicHolder>()));
        services.AddHeaderMessageVersionGetter<QueueStorageContext>();
        services.AddScoped<IMessageHeadersGetter<QueueStorageContext>, QueueStorageMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<QueueStorageContext>, QueueStorageMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<QueueStorageContext>, QueueStorageMessageHandlerResultSetter>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.QueueStorage));
        return services;
    }

    /// <summary>
    /// Adds a Queue Storage entry point application to the Azure Function app, configuring its inner
    /// middleware pipeline. Queue Storage messages carry no transport properties, so route them
    /// either with a Benzene message envelope in the body (<c>queue.UseBenzeneMessage(...)</c>) or a
    /// fixed per-queue topic (<c>queue.UsePresetTopic(...).UseMessageHandlers()</c>).
    /// </summary>
    /// <param name="app">The Azure Function app builder to add Queue Storage handling to.</param>
    /// <param name="action">The action that configures the Queue Storage middleware pipeline.</param>
    /// <returns>The Azure Function app builder, for method chaining.</returns>
    public static IAzureFunctionAppBuilder UseQueueStorage(this IAzureFunctionAppBuilder app, Action<IMiddlewarePipelineBuilder<QueueStorageContext>> action)
    {
        app.Register(x => x.AddAzureQueueStorage());
        var pipeline = app.Create<QueueStorageContext>();
        action(pipeline);
        app.Add(serviceResolverFactory => new QueueStorageApplication(pipeline.Build(), serviceResolverFactory));
        return app;
    }

    /// <summary>
    /// Applies Queue Storage-specific configuration to a platform-neutral <see cref="IBenzeneApplicationBuilder"/>.
    /// No-op on any platform other than Azure Functions.
    /// </summary>
    /// <param name="app">The application builder passed to <c>BenzeneStartUp.Configure</c>.</param>
    /// <param name="action">The action that configures the Queue Storage middleware pipeline.</param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IBenzeneApplicationBuilder UseQueueStorage(this IBenzeneApplicationBuilder app, Action<IMiddlewarePipelineBuilder<QueueStorageContext>> action)
    {
        if (app is IAzureFunctionAppBuilder azureApp)
        {
            azureApp.UseQueueStorage(action);
        }
        return app;
    }
}
