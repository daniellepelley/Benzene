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
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicGetter<SnsRecordContext>, SnsMessageTopicGetter>();
        services.AddScoped<IMessageVersionGetter<SnsRecordContext>, HeaderMessageVersionGetter<SnsRecordContext>>();
        services.AddScoped<IMessageHeadersGetter<SnsRecordContext>, SnsMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<SnsRecordContext>, SnsMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<SnsRecordContext>, SnsMessageHandlerResultSetter>();
        services.AddMediaFormatNegotiation<SnsRecordContext>();
        services
            .AddScoped<IRequestMapper<SnsRecordContext>,
                MultiSerializerOptionsRequestMapper<SnsRecordContext>>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("sns"));

        return services;
    }
}
