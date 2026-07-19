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

namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// Provides extension methods for registering SQS services.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to process SQS messages: request mapping, message
    /// extraction, and transport info.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="Extensions.UseSqs"/>; you don't normally need to call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddSqs(this IBenzeneServiceContainer services)
        => services.AddSqs(SqsMessageTopicGetter.DefaultTopicAttribute);

    /// <summary>
    /// Registers the services required to process SQS messages, with the topic getter reading the
    /// given message-attribute key (see <see cref="SqsMessageTopicGetter.DefaultTopicAttribute"/>).
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <param name="topicAttributeKey">The message attribute the topic is read from.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddSqs(this IBenzeneServiceContainer services, string topicAttributeKey)
    {
        services.TryAddScoped<JsonSerializer>();
        services.TryAddScoped<PresetTopicHolder>();

        services.AddScoped<IMessageTopicGetter<SqsMessageContext>>(resolver =>
            new PresetTopicMessageTopicGetter<SqsMessageContext>(new SqsMessageTopicGetter(topicAttributeKey), resolver.GetService<PresetTopicHolder>()));
        services.AddScoped<IMessageVersionGetter<SqsMessageContext>, HeaderMessageVersionGetter<SqsMessageContext>>();
        services.AddScoped<IMessageHeadersGetter<SqsMessageContext>, SqsMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<SqsMessageContext>, SqsMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<SqsMessageContext>, SqsMessageHandlerResultSetter>();
        services.AddMediaFormatNegotiation<SqsMessageContext>();
        services
            .AddScoped<IRequestMapper<SqsMessageContext>,
                MultiSerializerOptionsRequestMapper<SqsMessageContext>>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.Sqs));
        return services;
    }
}
