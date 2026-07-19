using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.Info;

namespace Benzene.Aws.Lambda.Kinesis;

/// <summary>
/// Provides extension methods for registering Kinesis Data Streams services.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to process Kinesis Data Streams events.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container, for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="Extensions.UseKinesisStream"/>. The streaming model consumes the
    /// batch directly via <c>UseStream(...)</c> rather than routing records to topic handlers, so unlike
    /// the SQS/SNS/S3 adapters there are no topic/body/header getters to register — only the transport info.
    /// </remarks>
    public static IBenzeneServiceContainer AddKinesis(this IBenzeneServiceContainer services)
    {
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.Kinesis));

        return services;
    }
}
