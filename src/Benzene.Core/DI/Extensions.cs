using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Core.Logging;

namespace Benzene.Core.DI;

/// <summary>
/// Provides extension methods for dependency injection configuration.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers default Benzene logging implementations into the service container.
    /// </summary>
    /// <param name="services">The service container to configure.</param>
    /// <returns>The service container for method chaining.</returns>
    /// <remarks>
    /// Registers <see cref="BenzeneLogger"/> as the singleton <see cref="IBenzeneLogger"/> implementation,
    /// <see cref="NullBenzeneLogContext"/> as the scoped <see cref="IBenzeneLogContext"/> implementation,
    /// and adds the service resolver.
    /// </remarks>
    public static IBenzeneServiceContainer AddDefaultBenzeneLogging(this IBenzeneServiceContainer services)
    {
        services.TryAddSingleton<IBenzeneLogger, BenzeneLogger>();
        services.TryAddScoped<IBenzeneLogContext, NullBenzeneLogContext>();
        services.AddServiceResolver();
        return services;
    }
}
