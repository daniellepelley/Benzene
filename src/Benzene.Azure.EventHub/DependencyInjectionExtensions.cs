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

namespace Benzene.Azure.EventHub;

/// <summary>
/// Provides extension methods for registering the standalone (non-Azure-Functions) Event Hub
/// consumer's services.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to process consumed events: message extraction and result
    /// recording.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="Extensions.UseEventHub"/>; you don't normally need to
    /// call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddEventHubConsumer(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();
        services.TryAddScoped<PresetTopicHolder>();

        services.AddScoped<IMessageTopicGetter<EventHubConsumerContext>>(resolver =>
            new PresetTopicMessageTopicGetter<EventHubConsumerContext>(new EventHubConsumerMessageTopicGetter(), resolver.GetService<PresetTopicHolder>()));
        services.AddScoped<IMessageVersionGetter<EventHubConsumerContext>, HeaderMessageVersionGetter<EventHubConsumerContext>>();
        services.AddScoped<IMessageHeadersGetter<EventHubConsumerContext>, EventHubConsumerMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<EventHubConsumerContext>, EventHubConsumerMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<EventHubConsumerContext>, EventHubConsumerMessageHandlerResultSetter>();
        services.AddMediaFormatNegotiation<EventHubConsumerContext>();
        services.AddScoped<IRequestMapper<EventHubConsumerContext>, MultiSerializerOptionsRequestMapper<EventHubConsumerContext>>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.EventHub));

        return services;
    }
}
