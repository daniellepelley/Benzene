using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Benzene.HealthChecks.Core;
using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;
using Xunit;

namespace Benzene.Mesh.Test;

public class MeshAggregatorTest : IDisposable
{
    private const string SpecUrl = "https://orders-api.example/spec?type=benzene";
    private const string HealthUrl = "https://orders-api.example/healthcheck";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), "benzene-mesh-aggregator-test-" + Guid.NewGuid());

    private static string SerializeHealth(bool isHealthy)
    {
        var response = new HealthCheckResponse(isHealthy, new Dictionary<string, HealthCheckResult>
        {
            { "Simple", (HealthCheckResult)HealthCheckResult.CreateInstance(isHealthy, "Simple") },
        });
        return JsonSerializer.Serialize(response, JsonOptions);
    }

    private MeshServiceRegistry SingleServiceRegistry() =>
        new(new[] { new MeshServiceRegistryEntry("orders-api", SpecUrl, HealthUrl) });

    [Fact]
    public async Task RunOnceAsync_HealthyService_FirstRun_ReportsHealthyNoDrift()
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, new FileSystemMeshArtifactStore(_rootDirectory));

        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        var entry = Assert.Single(manifest.Services);
        Assert.Equal(MeshServiceStatus.Healthy, entry.Status);
        Assert.False(entry.ContractDrift);
    }

    [Fact]
    public async Task RunOnceAsync_UnchangedSpec_SecondRun_NoDrift()
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);

        await aggregator.RunOnceAsync(SingleServiceRegistry());
        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.False(Assert.Single(manifest.Services).ContractDrift);
    }

    [Fact]
    public async Task RunOnceAsync_ChangedSpec_SecondRun_ReportsDrift()
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);

        await aggregator.RunOnceAsync(SingleServiceRegistry());

        handler.MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\",\"version\":\"2\"}}");
        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.True(Assert.Single(manifest.Services).ContractDrift);
    }

    [Fact]
    public async Task RunOnceAsync_HealthEndpointReportsUnhealthy_ManifestShowsUnhealthy()
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(false));
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, new FileSystemMeshArtifactStore(_rootDirectory));

        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.Equal(MeshServiceStatus.Unhealthy, Assert.Single(manifest.Services).Status);
    }

    [Fact]
    public async Task RunOnceAsync_HealthEndpointReports503Unhealthy_ManifestShowsUnhealthyNotUnreachable()
    {
        // Benzene.HealthChecks.HealthCheckProcessor.PerformHealthChecksAsync deliberately maps an
        // unhealthy aggregate result to HTTP 503 (ServiceUnavailable), not 200 - a real Benzene
        // health check reports "unhealthy" this way, not via a 200 with isHealthy:false in the
        // body. The aggregator must still read and deserialize that body instead of treating the
        // non-2xx status as an unreachable/fetch-failure case.
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.ServiceUnavailable, SerializeHealth(false));
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, new FileSystemMeshArtifactStore(_rootDirectory));

        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.Equal(MeshServiceStatus.Unhealthy, Assert.Single(manifest.Services).Status);
    }

    [Fact]
    public async Task RunOnceAsync_BothEndpointsFail_ManifestShowsUnreachable_ErrorIsTypeNameOnly()
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.InternalServerError, "connection string: secret-value")
            .MapGet(HealthUrl, HttpStatusCode.InternalServerError, "connection string: secret-value");
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);

        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.Equal(MeshServiceStatus.Unreachable, Assert.Single(manifest.Services).Status);

        var snapshotJson = await store.TryReadAsync("services/orders-api.json");
        Assert.NotNull(snapshotJson);
        var snapshot = JsonSerializer.Deserialize<MeshServiceSnapshot>(snapshotJson!, JsonOptions);
        Assert.NotNull(snapshot!.Error);
        Assert.DoesNotContain("secret-value", snapshot.Error);
        Assert.Equal(nameof(HttpRequestException), snapshot.Error);
    }

    [Fact]
    public async Task RunOnceAsync_OneServiceUnreachable_OtherHealthy_BothPublished()
    {
        const string otherSpecUrl = "https://payments-api.example/spec?type=benzene";
        const string otherHealthUrl = "https://payments-api.example/healthcheck";

        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.InternalServerError, null)
            .MapGet(HealthUrl, HttpStatusCode.InternalServerError, null)
            .MapGet(otherSpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"payments-api\"}}")
            .MapGet(otherHealthUrl, HttpStatusCode.OK, SerializeHealth(true));

        var registry = new MeshServiceRegistry(new[]
        {
            new MeshServiceRegistryEntry("orders-api", SpecUrl, HealthUrl),
            new MeshServiceRegistryEntry("payments-api", otherSpecUrl, otherHealthUrl),
        });
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, new FileSystemMeshArtifactStore(_rootDirectory));

        var manifest = await aggregator.RunOnceAsync(registry);

        Assert.Equal(2, manifest.Services.Length);
        Assert.Equal(MeshServiceStatus.Unreachable, manifest.Services.Single(x => x.Name == "orders-api").Status);
        Assert.Equal(MeshServiceStatus.Healthy, manifest.Services.Single(x => x.Name == "payments-api").Status);
    }

    [Fact]
    public async Task RunOnceAsync_EntryUsesNonHttpSource_ResolvesFromRegisteredSource_NotHttpClient()
    {
        // Proves the IMeshServiceSource seam is real: an entry with Source="fake" is fetched via
        // FakeMeshServiceSource below, never touching the HttpClient-backed HttpMeshServiceSource
        // also registered here - if MeshAggregator still had the fetch inlined, this would fail
        // (no stub configured for the fake entry's SpecUrl/HealthUrl on the HTTP handler).
        var handler = new RoutingHttpMessageHandler();
        var fakeSource = new FakeMeshServiceSource(
            "{\"info\":{\"title\":\"orders-api\"}}", SerializeHealth(true));
        var aggregator = new MeshAggregator(
            new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)), fakeSource },
            new FileSystemMeshArtifactStore(_rootDirectory));

        var registry = new MeshServiceRegistry(new[]
        {
            new MeshServiceRegistryEntry("orders-api", SpecUrl, HealthUrl, "fake", null),
        });

        var manifest = await aggregator.RunOnceAsync(registry);

        Assert.Equal(MeshServiceStatus.Healthy, Assert.Single(manifest.Services).Status);
        Assert.True(fakeSource.WasCalled);
    }

    [Fact]
    public async Task RunOnceAsync_EntryUsesUnregisteredSource_ManifestShowsUnreachable_DoesNotCrashOtherServices()
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var aggregator = new MeshAggregator(
            new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) },
            new FileSystemMeshArtifactStore(_rootDirectory));

        var registry = new MeshServiceRegistry(new[]
        {
            new MeshServiceRegistryEntry("orders-api", SpecUrl, HealthUrl),
            new MeshServiceRegistryEntry("payments-fn", "n/a", "n/a", "AwsLambdaInvoke", null),
        });

        var manifest = await aggregator.RunOnceAsync(registry);

        Assert.Equal(2, manifest.Services.Length);
        Assert.Equal(MeshServiceStatus.Healthy, manifest.Services.Single(x => x.Name == "orders-api").Status);
        Assert.Equal(MeshServiceStatus.Unreachable, manifest.Services.Single(x => x.Name == "payments-fn").Status);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    private class RoutingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode StatusCode, string? Content)> _responses = new();

        public RoutingHttpMessageHandler MapGet(string url, HttpStatusCode statusCode, string? content)
        {
            _responses[url] = (statusCode, content);
            return this;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            if (!_responses.TryGetValue(url, out var response))
            {
                throw new InvalidOperationException($"No stubbed response configured for {url}");
            }

            var message = new HttpResponseMessage(response.StatusCode);
            if (response.Content != null)
            {
                message.Content = new StringContent(response.Content);
            }

            return Task.FromResult(message);
        }
    }

    private class FakeMeshServiceSource : IMeshServiceSource
    {
        private readonly string _specJson;
        private readonly string _healthJson;

        public FakeMeshServiceSource(string specJson, string healthJson)
        {
            _specJson = specJson;
            _healthJson = healthJson;
        }

        public string Key => "fake";

        public bool WasCalled { get; private set; }

        public Task<string> FetchSpecAsync(MeshServiceRegistryEntry entry, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(_specJson);
        }

        public Task<string> FetchHealthAsync(MeshServiceRegistryEntry entry, CancellationToken cancellationToken)
        {
            return Task.FromResult(_healthJson);
        }
    }
}
