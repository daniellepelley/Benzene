using Amazon.S3;
using Benzene.Abstractions.DI;
using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Aws.S3;

/// <summary>Registration for the S3-backed mesh artifact store.</summary>
public static class Extensions
{
    /// <summary>
    /// Registers the mesh aggregator (<see cref="MeshAggregator"/>) backed by an
    /// <see cref="S3MeshArtifactStore"/> over a default-credential-chain <see cref="AmazonS3Client"/>,
    /// so catalog artifacts and the discovered registry live in the given S3 bucket.
    /// </summary>
    /// <param name="services">The service container.</param>
    /// <param name="registry">The initial registry (usually empty — discovery replaces it at runtime).</param>
    /// <param name="bucket">The S3 bucket artifacts are written to/read from.</param>
    /// <param name="prefix">An optional key prefix within the bucket.</param>
    public static IBenzeneServiceContainer AddMeshAggregatorWithS3(
        this IBenzeneServiceContainer services, MeshServiceRegistry registry, string bucket, string prefix = "")
    {
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client());
        return services.AddMeshAggregator(registry,
            resolver => new S3MeshArtifactStore(resolver.GetService<IAmazonS3>(), bucket, prefix));
    }
}
