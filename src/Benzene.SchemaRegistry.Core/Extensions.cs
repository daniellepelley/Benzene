using Benzene.Abstractions.DI;

namespace Benzene.SchemaRegistry.Core;

/// <summary>DI registration for a schema registry client.</summary>
public static class Extensions
{
    /// <summary>Registers <paramref name="client"/> as the <see cref="ISchemaRegistryClient"/> singleton.</summary>
    /// <param name="services">The service container.</param>
    /// <param name="client">The registry client to register.</param>
    public static IBenzeneServiceContainer AddSchemaRegistry(this IBenzeneServiceContainer services, ISchemaRegistryClient client)
    {
        services.AddSingleton<ISchemaRegistryClient>(client);
        return services;
    }
}
