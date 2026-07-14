using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Tracing.Tempo;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Mesh.Test;

public class TempoTopologyMessageHandlerTest : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), "benzene-mesh-tempo-test-" + Guid.NewGuid());

    private class FixedResponseHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _body;

        public FixedResponseHttpMessageHandler(string body)
        {
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_body) });
        }
    }

    [Fact]
    public async Task HandleAsync_PublishesTopologyJson_ToTheGivenStore()
    {
        var body = """
        {
          "status": "success",
          "data": {
            "resultType": "vector",
            "result": [
              { "metric": { "client": "orders-api", "server": "payments-api" }, "value": [1700000000, "12"] }
            ]
          }
        }
        """;
        var client = new PrometheusQueryClient(new HttpClient(new FixedResponseHttpMessageHandler(body)));
        var options = new TempoTopologyOptions("https://prometheus.example/api/v1/query");
        var builder = new TempoServiceGraphTopologyBuilder(client, options);
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var handler = new TempoTopologyMessageHandler(builder, store);

        var result = await handler.HandleAsync(new Void());

        Assert.True(result.IsSuccessful);
        var published = await store.TryReadAsync("topology.json");
        Assert.NotNull(published);

        var deserialized = JsonSerializer.Deserialize<MeshTopology>(published!, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
        var edge = Assert.Single(deserialized!.Edges);
        Assert.Equal("orders-api", edge.Client);
        Assert.Equal("payments-api", edge.Server);
        Assert.Equal(TopologyEdgeSource.Tempo, edge.Source);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}
