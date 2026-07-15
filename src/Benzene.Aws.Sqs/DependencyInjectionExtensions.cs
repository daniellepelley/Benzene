using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.MediaFormats;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Aws.Sqs;

/// <summary>
/// Provides extension methods for registering the standalone (non-Lambda) SQS polling consumer's services.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to poll and process SQS messages: the SQS client factory,
    /// message extraction, and request mapping.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="Extensions.UseSqs"/>; you don't normally need to call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddSqsConsumer(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<ISqsClientFactory, SqsClientFactory>();
        services.AddScoped<IMessageTopicGetter<SqsConsumerMessageContext>>(_ =>
            new PresetTopicMessageTopicGetter<SqsConsumerMessageContext>(new SqsConsumerMessageTopicGetter()));
        services.AddScoped<IMessageHeadersGetter<SqsConsumerMessageContext>, SqsConsumerMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<SqsConsumerMessageContext>, SqsConsumerMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<SqsConsumerMessageContext>, SqsConsumerMessageMessageHandlerResultSetter>();
        services.AddMediaFormatNegotiation<SqsConsumerMessageContext>();
        services
            .AddScoped<IRequestMapper<SqsConsumerMessageContext>,
                MultiSerializerOptionsRequestMapper<SqsConsumerMessageContext>>();

        return services;
    }
}
