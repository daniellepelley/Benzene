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

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}
