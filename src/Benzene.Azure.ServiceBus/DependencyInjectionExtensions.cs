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

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// Provides extension methods for registering the standalone (non-Azure-Functions) Service Bus
/// consumer's services.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to process consumed Service Bus messages: message extraction
    /// and result recording.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="Extensions.UseServiceBus"/>; you don't normally need to
    /// call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddServiceBusConsumer(this IBenzeneServiceContainer services)
        => services.AddServiceBusConsumer(ServiceBusConsumerMessageTopicGetter.DefaultTopicProperty);

    /// <summary>
    /// Registers the standalone Service Bus consumer's services, with the topic getter reading the
    /// given application-property key (see <see cref="ServiceBusConsumerMessageTopicGetter.DefaultTopicProperty"/>).
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <param name="topicPropertyKey">The application property the topic is read from.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddServiceBusConsumer(this IBenzeneServiceContainer services, string topicPropertyKey)
    {
        services.TryAddScoped<JsonSerializer>();
        services.TryAddScoped<PresetTopicHolder>();

        services.AddScoped<IMessageTopicGetter<ServiceBusConsumerContext>>(resolver =>
            new PresetTopicMessageTopicGetter<ServiceBusConsumerContext>(new ServiceBusConsumerMessageTopicGetter(topicPropertyKey), resolver.GetService<PresetTopicHolder>()));
        services.AddHeaderMessageVersionGetter<ServiceBusConsumerContext>();
        services.AddScoped<IMessageHeadersGetter<ServiceBusConsumerContext>, ServiceBusConsumerMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<ServiceBusConsumerContext>, ServiceBusConsumerMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<ServiceBusConsumerContext>, ServiceBusConsumerMessageHandlerResultSetter>();
        services.AddMediaFormatNegotiation<ServiceBusConsumerContext>();
        services.AddScoped<IRequestMapper<ServiceBusConsumerContext>, MultiSerializerOptionsRequestMapper<ServiceBusConsumerContext>>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.ServiceBus));

        return services;
    }
}
