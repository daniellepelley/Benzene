using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Benzene.Mesh.Aggregator;

namespace Benzene.Mesh.Azure.Blob;

/// <summary>
/// An <see cref="IMeshArtifactStore"/> backed by an Azure Blob Storage container. Publishes and reads
/// the mesh aggregator's generated artifacts (<c>manifest.json</c>, <c>services/*.json</c>,
/// <c>topology.json</c>, <c>topics.json</c>) and the discovery-generated <c>registry.json</c> as blobs
/// keyed by their relative path — the Azure analogue of <c>Benzene.Mesh.Aws.S3.S3MeshArtifactStore</c>,
/// so an Azure-hosted mesh persists its output centrally where the UI can read it.
/// </summary>
public class BlobMeshArtifactStore : IMeshArtifactStore
{
    private readonly BlobContainerClient _container;
    private readonly string _prefix;

    /// <summary>Initializes the store.</summary>
    /// <param name="container">The blob container artifacts live in.</param>
    /// <param name="prefix">An optional blob-name prefix (e.g. <c>"mesh/"</c>). Defaults to none.</param>
    public BlobMeshArtifactStore(BlobContainerClient container, string prefix = "")
    {
        _container = container;
        _prefix = string.IsNullOrEmpty(prefix) ? "" : prefix.TrimEnd('/') + "/";
    }

    /// <inheritdoc />
    public async Task PublishAsync(string relativePath, string content)
    {
        var blob = _container.GetBlobClient(Key(relativePath));
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" }
        });
    }

    /// <inheritdoc />
    public async Task<string?> TryReadAsync(string relativePath)
    {
        var blob = _container.GetBlobClient(Key(relativePath));
        try
        {
            var response = await blob.DownloadContentAsync();
            return response.Value.Content.ToString();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    private string Key(string relativePath) => _prefix + relativePath.Replace('\\', '/').TrimStart('/');
}
