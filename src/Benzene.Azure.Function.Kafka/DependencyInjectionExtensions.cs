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

namespace Benzene.Azure.Function.Kafka;

/// <summary>
/// Provides extension methods for registering Kafka message-handling services and adding Kafka trigger
/// handling to an <see cref="IAzureFunctionAppBuilder"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to process Kafka-triggered messages.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container, for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="UseKafka(IAzureFunctionAppBuilder, Action{IMiddlewarePipelineBuilder{KafkaContext}})"/>; you don't normally need to call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddAzureKafka(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicGetter<KafkaContext>, KafkaMessageTopicGetter>();
        services.AddScoped<IMessageVersionGetter<KafkaContext>, HeaderMessageVersionGetter<KafkaContext>>();
        services.AddScoped<IMessageHeadersGetter<KafkaContext>, KafkaMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<KafkaContext>, KafkaMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<KafkaContext>, KafkaMessageHandlerResultSetter>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("kafka"));
        return services;
    }

    /// <summary>
    /// Adds a Kafka entry point application to the Azure Function app, configuring its inner middleware
    /// pipeline.
    /// </summary>
    /// <param name="app">The Azure Function app builder to add Kafka handling to.</param>
    /// <param name="action">The action that configures the Kafka middleware pipeline.</param>
    /// <param name="configure">
    /// Optionally configures <see cref="KafkaOptions"/> - e.g. set <see cref="KafkaOptions.CatchExceptions"/>
    /// to contain a handler exception to the failing record instead of the default cascade-to-whole-invocation
    /// behavior, or <see cref="KafkaOptions.RaiseOnFailureStatus"/> to escalate a non-exception failure
    /// result into a thrown exception too.
    /// </param>
    /// <returns>The Azure Function app builder, for method chaining.</returns>
    public static IAzureFunctionAppBuilder UseKafka(this IAzureFunctionAppBuilder app, Action<IMiddlewarePipelineBuilder<KafkaContext>> action, Action<KafkaOptions>? configure = null)
    {
        app.Register(x => x.AddAzureKafka());
        var pipeline = app.Create<KafkaContext>();
        pipeline.UseBenzeneInvocation();
        action(pipeline);
        var options = new KafkaOptions();
        configure?.Invoke(options);
        app.Add(serviceResolverFactory => new KafkaApplication(pipeline.Build(), serviceResolverFactory, options));
        return app;
    }

    /// <summary>
    /// Applies Kafka-specific configuration to a platform-neutral <see cref="IBenzeneApplicationBuilder"/>.
    /// No-op on any platform other than Azure Functions.
    /// </summary>
    /// <param name="app">The application builder passed to <c>BenzeneStartUp.Configure</c>.</param>
    /// <param name="action">The action that configures the Kafka middleware pipeline.</param>
    /// <param name="configure">Optionally configures <see cref="KafkaOptions"/> - see the <see cref="IAzureFunctionAppBuilder"/> overload.</param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IBenzeneApplicationBuilder UseKafka(this IBenzeneApplicationBuilder app, Action<IMiddlewarePipelineBuilder<KafkaContext>> action, Action<KafkaOptions>? configure = null)
    {
        if (app is IAzureFunctionAppBuilder azureApp)
        {
            azureApp.UseKafka(action, configure);
        }
        return app;
    }
}
