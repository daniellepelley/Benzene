using System;
using System.IO;
using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Mesh.Test;

public class MeshAggregateMessageHandlerTest : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), "benzene-mesh-aggregate-handler-test-" + Guid.NewGuid());

    [Fact]
    public async Task HandleAsync_DelegatesToAggregatorRunOnceAsync_UsingTheRegisteredRegistry()
    {
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(Array.Empty<IMeshServiceSource>(), store);
        var registry = new MeshServiceRegistry(Array.Empty<MeshServiceRegistryEntry>());
        var handler = new MeshAggregateMessageHandler(aggregator, registry);

        var result = await handler.HandleAsync(new Void());

        Assert.True(result.IsSuccessful);
        Assert.Empty(result.Payload.Services);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}
