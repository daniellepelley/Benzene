using Benzene.Abstractions.DI;

namespace Benzene.Configuration.Core;

/// <summary>
/// DI registration for a secret store. Registers the <see cref="ISecretStore"/> and a
/// <see cref="SecretResolver"/> over it as singletons, so handlers and startup code can resolve
/// either.
/// </summary>
public static class Extensions
{
    /// <summary>Registers <paramref name="store"/> as the <see cref="ISecretStore"/>, plus a <see cref="SecretResolver"/> over it.</summary>
    /// <param name="services">The service container.</param>
    /// <param name="store">The store to register.</param>
    public static IBenzeneServiceContainer AddSecretStore(this IBenzeneServiceContainer services, ISecretStore store)
    {
        services.AddSingleton<ISecretStore>(store);
        services.AddSingleton(new SecretResolver(store));
        return services;
    }

    /// <summary>
    /// Registers an ordered <see cref="CompositeSecretStore"/> (first non-null wins) as the
    /// <see cref="ISecretStore"/>, plus a <see cref="SecretResolver"/> over it.
    /// </summary>
    /// <param name="services">The service container.</param>
    /// <param name="stores">The stores to try in order.</param>
    public static IBenzeneServiceContainer AddSecretStores(this IBenzeneServiceContainer services, params ISecretStore[] stores)
        => services.AddSecretStore(new CompositeSecretStore(stores));
}
