using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Aws.Lambda.EventBridge;

/// <summary>
/// Provides extension methods for registering EventBridge message handling services.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to route EventBridge events to message handlers.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container, for method chaining.</returns>
    public static IBenzeneServiceContainer AddEventBridge(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicGetter<EventBridgeContext>, EventBridgeMessageTopicGetter>();
        services.AddScoped<IMessageHeadersGetter<EventBridgeContext>, EventBridgeMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<EventBridgeContext>, EventBridgeMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<EventBridgeContext>, EventBridgeMessageMessageHandlerResultSetter>();
        services
            .AddScoped<IRequestMapper<EventBridgeContext>,
                MultiSerializerOptionsRequestMapper<EventBridgeContext, JsonSerializer>>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("eventbridge"));

        return services;
    }
}
