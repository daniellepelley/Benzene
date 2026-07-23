using Benzene.Abstractions.DI;
using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;
using Google.Cloud.Storage.V1;

namespace Benzene.Mesh.GoogleCloud.Storage;

/// <summary>Registration for the GCS-backed mesh artifact store.</summary>
public static class Extensions
{
    /// <summary>
    /// Registers the mesh aggregator (<see cref="MeshAggregator"/>) backed by a
    /// <see cref="GcsMeshArtifactStore"/> over the given bucket, so catalog artifacts and the discovered
    /// registry live in Google Cloud Storage. Mirrors <c>AddMeshAggregatorWithS3</c> /
    /// <c>AddMeshAggregatorWithBlob</c>.
    /// </summary>
    /// <param name="services">The service container.</param>
    /// <param name="registry">The initial registry (usually empty — discovery replaces it at runtime).</param>
    /// <param name="storage">The GCS client.</param>
    /// <param name="bucket">The bucket artifacts are written to/read from.</param>
    /// <param name="prefix">An optional object-name prefix within the bucket.</param>
    public static IBenzeneServiceContainer AddMeshAggregatorWithGcs(
        this IBenzeneServiceContainer services, MeshServiceRegistry registry, StorageClient storage, string bucket, string prefix = "")
    {
        return services.AddMeshAggregator(registry, _ => new GcsMeshArtifactStore(storage, bucket, prefix));
    }

    /// <summary>
    /// Convenience overload that builds the <see cref="StorageClient"/> from Application Default
    /// Credentials (the attached service account in Google Cloud, the dev credential locally).
    /// </summary>
    /// <param name="services">The service container.</param>
    /// <param name="registry">The initial registry (usually empty — discovery replaces it at runtime).</param>
    /// <param name="bucket">The bucket artifacts are written to/read from.</param>
    /// <param name="prefix">An optional object-name prefix within the bucket.</param>
    public static IBenzeneServiceContainer AddMeshAggregatorWithGcs(
        this IBenzeneServiceContainer services, MeshServiceRegistry registry, string bucket, string prefix = "")
    {
        return services.AddMeshAggregatorWithGcs(registry, StorageClient.Create(), bucket, prefix);
    }
}
