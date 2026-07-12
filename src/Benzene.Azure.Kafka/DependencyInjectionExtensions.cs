using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Core;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Azure.Kafka;

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
    /// Called automatically by <see cref="UseKafka"/>; you don't normally need to call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddAzureKafka(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicGetter<KafkaContext>, KafkaMessageTopicGetter>();
        services.AddScoped<IMessageHeadersGetter<KafkaContext>, KafkaMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<KafkaContext>, KafkaMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<KafkaContext>, KafkaMessageMessageHandlerResultSetter>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("kafka"));
        return services;
    }

    /// <summary>
    /// Adds a Kafka entry point application to the Azure Function app, configuring its inner middleware
    /// pipeline.
    /// </summary>
    /// <param name="app">The Azure Function app builder to add Kafka handling to.</param>
    /// <param name="action">The action that configures the Kafka middleware pipeline.</param>
    /// <returns>The Azure Function app builder, for method chaining.</returns>
    public static IAzureFunctionAppBuilder UseKafka(this IAzureFunctionAppBuilder app, Action<IMiddlewarePipelineBuilder<KafkaContext>> action)
    {
        app.Register(x => x.AddAzureKafka());
        var pipeline = app.Create<KafkaContext>();
        action(pipeline);
        app.Add(serviceResolverFactory => new KafkaApplication(pipeline.Build(), serviceResolverFactory));
        return app;
    }
}
