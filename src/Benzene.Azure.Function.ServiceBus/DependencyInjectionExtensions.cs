using Azure.Messaging.ServiceBus;
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
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// Provides extension methods for registering Service Bus message-handling services and adding Service
/// Bus trigger handling to an <see cref="IAzureFunctionAppBuilder"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to process Service Bus-triggered messages.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container, for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="UseServiceBus(IAzureFunctionAppBuilder, Action{IMiddlewarePipelineBuilder{ServiceBusContext}})"/>; you don't normally need to call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddAzureServiceBus(this IBenzeneServiceContainer services)
        => services.AddAzureServiceBus(ServiceBusMessageTopicGetter.DefaultTopicProperty);

    /// <summary>
    /// Registers the services required to process Service Bus-triggered messages, with the topic getter
    /// reading the given application-property key (see <see cref="ServiceBusMessageTopicGetter.DefaultTopicProperty"/>).
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <param name="topicPropertyKey">The application property the topic is read from.</param>
    /// <returns>The service container, for method chaining.</returns>
    public static IBenzeneServiceContainer AddAzureServiceBus(this IBenzeneServiceContainer services, string topicPropertyKey)
    {
        services.TryAddScoped<JsonSerializer>();
        services.TryAddScoped<PresetTopicHolder>();

        services.AddScoped<IMessageTopicGetter<ServiceBusContext>>(resolver =>
            new PresetTopicMessageTopicGetter<ServiceBusContext>(new ServiceBusMessageTopicGetter(topicPropertyKey), resolver.GetService<PresetTopicHolder>()));
        services.AddScoped<IMessageVersionGetter<ServiceBusContext>, HeaderMessageVersionGetter<ServiceBusContext>>();
        services.AddScoped<IMessageHeadersGetter<ServiceBusContext>, ServiceBusMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<ServiceBusContext>, ServiceBusMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<ServiceBusContext>, ServiceBusMessageHandlerResultSetter>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.ServiceBus));
        return services;
    }

    /// <summary>
    /// Adds a Service Bus entry point application to the Azure Function app, configuring its inner
    /// middleware pipeline.
    /// </summary>
    /// <param name="app">The Azure Function app builder to add Service Bus handling to.</param>
    /// <param name="action">The action that configures the Service Bus middleware pipeline.</param>
    /// <param name="configure">
    /// Optionally configures <see cref="ServiceBusOptions"/> - e.g. set
    /// <see cref="ServiceBusOptions.CatchExceptions"/> to contain a handler exception to the
    /// failing message instead of the default cascade-to-whole-invocation behavior, or
    /// <see cref="ServiceBusOptions.RaiseOnFailureStatus"/> to escalate a non-exception failure
    /// result into a thrown exception too.
    /// </param>
    /// <returns>The Azure Function app builder, for method chaining.</returns>
    public static IAzureFunctionAppBuilder UseServiceBus(this IAzureFunctionAppBuilder app, Action<IMiddlewarePipelineBuilder<ServiceBusContext>> action, Action<ServiceBusOptions>? configure = null, string topicPropertyKey = ServiceBusMessageTopicGetter.DefaultTopicProperty)
    {
        app.Register(x => x.AddAzureServiceBus(topicPropertyKey));
        var pipeline = app.Create<ServiceBusContext>();
        action(pipeline);
        var options = new ServiceBusOptions();
        configure?.Invoke(options);

        // One ServiceBusBatchApplication instance (it holds no per-invocation state - only the
        // fixed pipeline/options), wrapped by two entry points so a trigger function can call
        // whichever HandleServiceBusMessages overload it needs (with or without
        // ServiceBusMessageActions) - see ServiceBusTriggerBatch's own doc comment for why this is
        // a second registration rather than a parameter on the existing one.
        var batchApplication = new ServiceBusBatchApplication(pipeline.Build(), options);
        app.Add(serviceResolverFactory => new EntryPointMiddlewareApplication<ServiceBusReceivedMessage[]>(batchApplication, serviceResolverFactory));
        app.Add(serviceResolverFactory => new EntryPointMiddlewareApplication<ServiceBusTriggerBatch>(batchApplication, serviceResolverFactory));
        return app;
    }

    /// <summary>
    /// Applies Service Bus-specific configuration to a platform-neutral <see cref="IBenzeneApplicationBuilder"/>.
    /// No-op on any platform other than Azure Functions.
    /// </summary>
    /// <param name="app">The application builder passed to <c>BenzeneStartUp.Configure</c>.</param>
    /// <param name="action">The action that configures the Service Bus middleware pipeline.</param>
    /// <param name="configure">Optionally configures <see cref="ServiceBusOptions"/> - see the <see cref="IAzureFunctionAppBuilder"/> overload.</param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IBenzeneApplicationBuilder UseServiceBus(this IBenzeneApplicationBuilder app, Action<IMiddlewarePipelineBuilder<ServiceBusContext>> action, Action<ServiceBusOptions>? configure = null, string topicPropertyKey = ServiceBusMessageTopicGetter.DefaultTopicProperty)
    {
        if (app is IAzureFunctionAppBuilder azureApp)
        {
            azureApp.UseServiceBus(action, configure, topicPropertyKey);
        }
        return app;
    }
}
