using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Provides extension methods for registering Kafka services.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to process Kafka records: message extraction and transport info.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="Extensions.UseKafka"/>; you don't normally need to call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddKafka(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicGetter<KafkaContext>, KafkaMessageTopicGetter>();
        services.AddScoped<IMessageVersionGetter<KafkaContext>, HeaderMessageVersionGetter<KafkaContext>>();
        services.AddScoped<IMessageHeadersGetter<KafkaContext>, KafkaMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<KafkaContext>, KafkaMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<KafkaContext>, KafkaMessageHandlerResultSetter>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.Kafka));
        return services;
    }
}
