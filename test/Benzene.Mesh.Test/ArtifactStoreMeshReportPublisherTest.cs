using System.Text.Json;
using Benzene.HealthChecks.Core;
using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;
using Xunit;

namespace Benzene.Mesh.Test;

public class ArtifactStoreMeshReportPublisherTest : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), "benzene-mesh-report-publisher-test-" + Guid.NewGuid());

    private static HealthCheckResponse HealthyResponse() =>
        new(true, new Dictionary<string, HealthCheckResult> { { "Simple", (HealthCheckResult)HealthCheckResult.CreateInstance(true, "Simple") } });

    [Fact]
    public async Task PublishAsync_WritesSnapshotToArtifactStore()
    {
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var publisher = new ArtifactStoreMeshReportPublisher(store);
        var report = new MeshServiceReport("payments-fn", DateTimeOffset.UtcNow, "{\"info\":{\"title\":\"payments-fn\"}}", HealthyResponse(), null);

        await publisher.PublishAsync(report);

        var snapshotJson = await store.TryReadAsync("services/payments-fn.json");
        Assert.NotNull(snapshotJson);
        var snapshot = JsonSerializer.Deserialize<MeshServiceSnapshot>(snapshotJson!, JsonOptions);
        Assert.Equal("payments-fn", snapshot!.Name);
        Assert.True(snapshot.Health!.IsHealthy);
        Assert.False(snapshot.ContractDrift);
    }

    [Fact]
    public async Task PublishAsync_SecondReportWithChangedSpec_DetectsDrift_SameAsAPulledFetchWould()
    {
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var publisher = new ArtifactStoreMeshReportPublisher(store);

        await publisher.PublishAsync(new MeshServiceReport("payments-fn", DateTimeOffset.UtcNow, "{\"info\":{\"title\":\"v1\"}}", HealthyResponse(), null));
        await publisher.PublishAsync(new MeshServiceReport("payments-fn", DateTimeOffset.UtcNow, "{\"info\":{\"title\":\"v2\"}}", HealthyResponse(), null));

        var snapshotJson = await store.TryReadAsync("services/payments-fn.json");
        var snapshot = JsonSerializer.Deserialize<MeshServiceSnapshot>(snapshotJson!, JsonOptions);

        Assert.True(snapshot!.ContractDrift);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}
