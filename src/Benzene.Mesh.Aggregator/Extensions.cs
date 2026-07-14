using Benzene.Abstractions.DI;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Aggregator;

/// <summary>
/// Provides extension methods for registering the mesh aggregator with a Benzene service container.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers <see cref="MeshAggregator"/>, backed by a <see cref="FileSystemMeshArtifactStore"/>,
    /// against the given service registry. Handler discovery for
    /// <see cref="MeshAggregateMessageHandler"/> is left to the consuming app's own
    /// <c>.AddMessageHandlers()</c> call, matching how every other Benzene message handler is
    /// discovered.
    /// </summary>
    /// <param name="services">The service container to register with.</param>
    /// <param name="registry">The services the aggregator should poll on each run.</param>
    /// <param name="artifactRootDirectory">The local directory generated catalog artifacts are written to.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddMeshAggregator(
        this IBenzeneServiceContainer services, MeshServiceRegistry registry, string artifactRootDirectory)
    {
        services.AddSingleton(registry);
        services.AddSingleton<IMeshArtifactStore>(_ => new FileSystemMeshArtifactStore(artifactRootDirectory));
        services.AddSingleton<HttpClient>();
        services.AddSingleton<MeshAggregator>();
        return services;
    }
}
