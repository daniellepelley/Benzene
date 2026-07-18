using Azure.Identity;
using Azure.Storage.Blobs;
using Benzene.Abstractions.DI;
using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Azure.Blob;

/// <summary>Registration for the Azure-Blob-backed mesh artifact store.</summary>
public static class Extensions
{
    /// <summary>
    /// Registers the mesh aggregator (<see cref="MeshAggregator"/>) backed by a
    /// <see cref="BlobMeshArtifactStore"/> over the given container, so catalog artifacts and the
    /// discovered registry live in Azure Blob Storage. Mirrors
    /// <c>Benzene.Mesh.Aws.S3.AddMeshAggregatorWithS3</c>.
    /// </summary>
    /// <param name="services">The service container.</param>
    /// <param name="registry">The initial registry (usually empty — discovery replaces it at runtime).</param>
    /// <param name="container">The blob container artifacts are written to/read from.</param>
    /// <param name="prefix">An optional blob-name prefix within the container.</param>
    public static IBenzeneServiceContainer AddMeshAggregatorWithBlob(
        this IBenzeneServiceContainer services, MeshServiceRegistry registry, BlobContainerClient container, string prefix = "")
    {
        return services.AddMeshAggregator(registry, _ => new BlobMeshArtifactStore(container, prefix));
    }

    /// <summary>
    /// Convenience overload that builds the <see cref="BlobContainerClient"/> from a blob service URI
    /// and container name, authenticated with <see cref="DefaultAzureCredential"/> (managed identity in
    /// Azure, the dev credential locally).
    /// </summary>
    /// <param name="services">The service container.</param>
    /// <param name="registry">The initial registry (usually empty — discovery replaces it at runtime).</param>
    /// <param name="blobServiceUri">The storage account's blob endpoint, e.g. <c>https://acct.blob.core.windows.net</c>.</param>
    /// <param name="containerName">The container name.</param>
    /// <param name="prefix">An optional blob-name prefix within the container.</param>
    public static IBenzeneServiceContainer AddMeshAggregatorWithBlob(
        this IBenzeneServiceContainer services, MeshServiceRegistry registry, Uri blobServiceUri, string containerName, string prefix = "")
    {
        var container = new BlobServiceClient(blobServiceUri, new DefaultAzureCredential())
            .GetBlobContainerClient(containerName);
        return services.AddMeshAggregatorWithBlob(registry, container, prefix);
    }
}
