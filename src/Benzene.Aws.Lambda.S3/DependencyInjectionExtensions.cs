using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Aws.S3;

/// <summary>
/// Provides extension methods for registering S3 event notification services.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to process S3 event notifications: topic/body/header
    /// extraction, request mapping, and transport info, so S3 records can be routed to message
    /// handlers by their event name.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="Extensions.UseS3"/>; you don't normally need to call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddS3(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicGetter<S3RecordContext>, S3MessageTopicGetter>();
        services.AddScoped<IMessageHeadersGetter<S3RecordContext>, S3MessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<S3RecordContext>, S3MessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<S3RecordContext>, S3MessageMessageHandlerResultSetter>();
        services
            .AddScoped<IRequestMapper<S3RecordContext>,
                MultiSerializerOptionsRequestMapper<S3RecordContext, JsonSerializer>>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("s3"));

        return services;
    }
}

