using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.MediaFormats;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.RabbitMq.RabbitMqMessage;

namespace Benzene.RabbitMq;

/// <summary>
/// Registers the services required to consume RabbitMQ deliveries through a Benzene pipeline.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers everything <c>.UseMessageHandlers()</c> resolves per <see cref="RabbitMqContext"/>:
    /// the topic/version/headers/body getters, the result setter, media-format negotiation, and the
    /// request mapper. Called automatically by <see cref="Extensions.UseRabbitMq"/>.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddRabbitMq(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();
        services.TryAddScoped<PresetTopicHolder>();

        services.AddScoped<IMessageTopicGetter<RabbitMqContext>>(resolver =>
            new PresetTopicMessageTopicGetter<RabbitMqContext>(new RabbitMqMessageTopicGetter(), resolver.GetService<PresetTopicHolder>()));
        services.AddScoped<IMessageVersionGetter<RabbitMqContext>, HeaderMessageVersionGetter<RabbitMqContext>>();
        services.AddScoped<IMessageHeadersGetter<RabbitMqContext>, RabbitMqMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<RabbitMqContext>, RabbitMqMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<RabbitMqContext>, RabbitMqMessageHandlerResultSetter>();
        services.AddMediaFormatNegotiation<RabbitMqContext>();
        services.AddScoped<IRequestMapper<RabbitMqContext>, MultiSerializerOptionsRequestMapper<RabbitMqContext>>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.RabbitMq));

        return services;
    }
}
