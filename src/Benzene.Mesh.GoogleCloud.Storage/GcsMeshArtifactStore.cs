using System.Text;
using Benzene.Mesh.Aggregator;
using Google;
using Google.Cloud.Storage.V1;

namespace Benzene.Mesh.GoogleCloud.Storage;

/// <summary>
/// An <see cref="IMeshArtifactStore"/> backed by a Google Cloud Storage bucket. Publishes and reads the
/// mesh aggregator's generated artifacts (<c>manifest.json</c>, <c>services/*.json</c>,
/// <c>topology.json</c>, <c>topics.json</c>) and the discovery-generated <c>registry.json</c> as GCS
/// objects keyed by their relative path — the Google Cloud analogue of
/// <c>Benzene.Mesh.Aws.S3.S3MeshArtifactStore</c> / <c>Benzene.Mesh.Azure.Blob.BlobMeshArtifactStore</c>,
/// so a Cloud-Functions-hosted mesh persists its output centrally where the UI can read it (surviving
/// cold starts / scale-to-zero).
/// </summary>
public class GcsMeshArtifactStore : IMeshArtifactStore
{
    private readonly StorageClient _storage;
    private readonly string _bucket;
    private readonly string _prefix;

    /// <summary>Initializes the store.</summary>
    /// <param name="storage">The GCS client.</param>
    /// <param name="bucket">The bucket artifacts live in.</param>
    /// <param name="prefix">An optional object-name prefix (e.g. <c>"mesh/"</c>). Defaults to none.</param>
    public GcsMeshArtifactStore(StorageClient storage, string bucket, string prefix = "")
    {
        _storage = storage;
        _bucket = bucket;
        _prefix = string.IsNullOrEmpty(prefix) ? "" : prefix.TrimEnd('/') + "/";
    }

    /// <inheritdoc />
    public async Task PublishAsync(string relativePath, string content)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await _storage.UploadObjectAsync(_bucket, Key(relativePath), "application/json", stream);
    }

    /// <inheritdoc />
    public async Task<string?> TryReadAsync(string relativePath)
    {
        using var stream = new MemoryStream();
        try
        {
            await _storage.DownloadObjectAsync(_bucket, Key(relativePath), stream);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private string Key(string relativePath) => _prefix + relativePath.Replace('\\', '/').TrimStart('/');
}
