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

namespace Benzene.Aws.Lambda.Sns;

/// <summary>
/// Provides extension methods for registering SNS services.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to process SNS notifications: request mapping, message
    /// extraction, and transport info.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="Extensions.UseSns"/>; you don't normally need to call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddSns(this IBenzeneServiceContainer services)
        => services.AddSns(SnsMessageTopicGetter.DefaultTopicAttribute);

    /// <summary>
    /// Registers the services required to process SNS notifications, with the topic getter reading the
    /// given message-attribute key (see <see cref="SnsMessageTopicGetter.DefaultTopicAttribute"/>).
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <param name="topicAttributeKey">The message attribute the topic is read from.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddSns(this IBenzeneServiceContainer services, string topicAttributeKey)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicGetter<SnsRecordContext>>(_ => new SnsMessageTopicGetter(topicAttributeKey));
        services.AddHeaderMessageVersionGetter<SnsRecordContext>();
        services.AddScoped<IMessageHeadersGetter<SnsRecordContext>, SnsMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<SnsRecordContext>, SnsMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<SnsRecordContext>, SnsMessageHandlerResultSetter>();
        services.AddMediaFormatNegotiation<SnsRecordContext>();
        services
            .AddScoped<IRequestMapper<SnsRecordContext>,
                MultiSerializerOptionsRequestMapper<SnsRecordContext>>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.Sns));

        return services;
    }
}
