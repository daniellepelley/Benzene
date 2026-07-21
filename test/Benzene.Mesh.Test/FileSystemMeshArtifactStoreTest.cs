using System;
using System.IO;
using System.Threading.Tasks;
using Benzene.Mesh.Aggregator;
using Xunit;

namespace Benzene.Mesh.Test;

public class FileSystemMeshArtifactStoreTest : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), "benzene-mesh-test-" + Guid.NewGuid());

    [Fact]
    public async Task TryReadAsync_NothingPublished_ReturnsNull()
    {
        var store = new FileSystemMeshArtifactStore(_rootDirectory);

        Assert.Null(await store.TryReadAsync("manifest.json"));
    }

    [Fact]
    public async Task PublishAsync_ThenTryReadAsync_RoundTrips()
    {
        var store = new FileSystemMeshArtifactStore(_rootDirectory);

        await store.PublishAsync("manifest.json", "{\"hello\":\"world\"}");

        Assert.Equal("{\"hello\":\"world\"}", await store.TryReadAsync("manifest.json"));
    }

    [Fact]
    public async Task PublishAsync_NestedRelativePath_CreatesDirectory()
    {
        var store = new FileSystemMeshArtifactStore(_rootDirectory);

        await store.PublishAsync("services/orders-api.json", "{}");

        Assert.Equal("{}", await store.TryReadAsync("services/orders-api.json"));
    }

    [Fact]
    public async Task PublishAsync_Overwrite_ReplacesContent()
    {
        var store = new FileSystemMeshArtifactStore(_rootDirectory);

        await store.PublishAsync("manifest.json", "{\"version\":1}");
        await store.PublishAsync("manifest.json", "{\"version\":2}");

        Assert.Equal("{\"version\":2}", await store.TryReadAsync("manifest.json"));
    }

    [Theory]
    [InlineData("../escape.json")]
    [InlineData("services/../../escape.json")]
    [InlineData("../../etc/passwd")]
    public async Task PublishAsync_PathEscapingRoot_IsRejected(string relativePath)
    {
        // The relative path can carry a service name from an untrusted push report, so a traversal
        // sequence must not let a write escape the store root.
        var store = new FileSystemMeshArtifactStore(_rootDirectory);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => store.PublishAsync(relativePath, "{}"));
    }

    [Fact]
    public async Task TryReadAsync_PathEscapingRoot_IsRejected()
    {
        var store = new FileSystemMeshArtifactStore(_rootDirectory);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => store.TryReadAsync("../../etc/passwd"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}
