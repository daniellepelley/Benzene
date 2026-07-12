using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.Info;

namespace Benzene.Aws.EventBridge;

/// <summary>
/// Provides extension methods for registering S3 event notification services.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to process S3 event notifications.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="Extensions.UseS3"/>; you don't normally need to call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddS3(this IBenzeneServiceContainer services)
    {
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("s3"));

        return services;
    }
}

