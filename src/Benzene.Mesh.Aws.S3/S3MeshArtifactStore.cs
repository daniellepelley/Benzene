using Amazon.S3;
using Amazon.S3.Model;
using Benzene.Mesh.Aggregator;

namespace Benzene.Mesh.Aws.S3;

/// <summary>
/// An <see cref="IMeshArtifactStore"/> backed by an Amazon S3 bucket. Publishes and reads the mesh
/// aggregator's generated artifacts (<c>manifest.json</c>, <c>services/*.json</c>, <c>topology.json</c>)
/// and the discovery-generated <c>registry.json</c> as objects keyed by their relative path, so a mesh
/// service running as a Lambda persists its output centrally where the UI can read it.
/// </summary>
public class S3MeshArtifactStore : IMeshArtifactStore
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly string _prefix;

    /// <summary>Initializes the store.</summary>
    /// <param name="s3">The S3 client.</param>
    /// <param name="bucket">The bucket artifacts live in.</param>
    /// <param name="prefix">An optional key prefix (e.g. <c>"mesh/"</c>). Defaults to none.</param>
    public S3MeshArtifactStore(IAmazonS3 s3, string bucket, string prefix = "")
    {
        _s3 = s3;
        _bucket = bucket;
        _prefix = string.IsNullOrEmpty(prefix) ? "" : prefix.TrimEnd('/') + "/";
    }

    /// <inheritdoc />
    public async Task PublishAsync(string relativePath, string content)
    {
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = Key(relativePath),
            ContentBody = content,
            ContentType = "application/json"
        });
    }

    /// <inheritdoc />
    public async Task<string?> TryReadAsync(string relativePath)
    {
        try
        {
            using var response = await _s3.GetObjectAsync(_bucket, Key(relativePath));
            using var reader = new StreamReader(response.ResponseStream);
            return await reader.ReadToEndAsync();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private string Key(string relativePath) => _prefix + relativePath.Replace('\\', '/').TrimStart('/');
}
