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
        => services.AddMeshAggregator(registry, _ => new FileSystemMeshArtifactStore(artifactRootDirectory));

    /// <summary>
    /// Registers <see cref="MeshAggregator"/> backed by a caller-supplied <see cref="IMeshArtifactStore"/>
    /// factory — for a blob-storage adapter (S3, Azure Blob) instead of the local-disk default.
    /// </summary>
    /// <param name="services">The service container to register with.</param>
    /// <param name="registry">The services the aggregator should poll on each run.</param>
    /// <param name="artifactStoreFactory">Builds the store generated catalog artifacts are written to/read from.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddMeshAggregator(
        this IBenzeneServiceContainer services, MeshServiceRegistry registry,
        Func<IServiceResolver, IMeshArtifactStore> artifactStoreFactory)
    {
        services.AddSingleton(registry);
        services.AddSingleton(artifactStoreFactory);
        services.AddSingleton<HttpClient>();
        // The default IMeshServiceSource - other adapter packages (e.g. an AWS Lambda Invoke
        // source) add their own IMeshServiceSource registration alongside this one; MeshAggregator
        // resolves all of them via IEnumerable<IMeshServiceSource>, keyed by each source's Key.
        services.AddSingleton<IMeshServiceSource>(resolver => new HttpMeshServiceSource(resolver.GetService<HttpClient>()));
        // The default push-path IMeshReportPublisher - MeshReportMessageHandler resolves this, but
        // only becomes reachable if the host's own .AddMessageHandlers() discovers it.
        services.AddSingleton<IMeshReportPublisher>(resolver => new ArtifactStoreMeshReportPublisher(resolver.GetService<IMeshArtifactStore>()));
        services.AddSingleton<MeshAggregator>();
        return services;
    }
}
