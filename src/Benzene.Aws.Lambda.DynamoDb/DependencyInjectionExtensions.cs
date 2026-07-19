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

namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// Provides extension methods for registering DynamoDB Streams services.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to process DynamoDB stream records: request mapping,
    /// message extraction, and transport info.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="Extensions.UseDynamoDb"/>; you don't normally need to call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddDynamoDb(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicGetter<DynamoDbRecordContext>, DynamoDbMessageTopicGetter>();
        services.AddHeaderMessageVersionGetter<DynamoDbRecordContext>();
        services.AddScoped<IMessageHeadersGetter<DynamoDbRecordContext>, DynamoDbMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<DynamoDbRecordContext>, DynamoDbMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<DynamoDbRecordContext>, DynamoDbMessageHandlerResultSetter>();
        services.AddMediaFormatNegotiation<DynamoDbRecordContext>();
        services
            .AddScoped<IRequestMapper<DynamoDbRecordContext>,
                MultiSerializerOptionsRequestMapper<DynamoDbRecordContext>>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.DynamoDb));
        return services;
    }
}
