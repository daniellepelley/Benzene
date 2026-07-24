using System.Threading;
using System.Threading.Tasks;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.ApiGateway.TestHelpers;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.TestHelpers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Http;
using Benzene.Http.BenzeneMessage;
using Benzene.Mesh.Collector;
using Benzene.Mesh.Ui;
using Benzene.Microsoft.Dependencies;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Mesh.Test;

/// <summary>
/// End-to-end regression guard for the AwsMesh fleet endpoint wiring (the composite-backed fleet plane):
/// the exact shape <c>examples/AwsMesh/Mesh/Startup.cs</c> wires — a <c>/benzene/invoke</c> BenzeneMessage
/// endpoint routing <see cref="MeshCollectorHandlers.Queries"/> over an <see cref="IMeshFleetReadModel"/>,
/// with the mesh UI's live Fleet plane folded into <c>UseMeshUi(..., envelopeUrl)</c> and a fall-through
/// <c>UseMessageHandlers</c> — must answer the Fleet plane's <c>POST /benzene/invoke</c>
/// <c>mesh:query:fleet</c> poll with 200, through the real (payload format 1.0) API Gateway host. Pins the
/// "collector unreachable (HTTP 404)" regression: examples aren't in the CI gate, so this library-side test
/// stands in for that wiring.
/// </summary>
public class AwsMeshFleetEndpointTest
{
    private sealed class FakeReadModel : IMeshFleetReadModel
    {
        public Task<FleetView> FleetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new FleetView());
        public Task<ServiceView?> ServiceAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult<ServiceView?>(null);
        public Task<TopicSummary?> TopicAsync(string id, string? version, CancellationToken cancellationToken = default)
            => Task.FromResult<TopicSummary?>(null);
        public Task<TraceView?> TraceAsync(string traceId, CancellationToken cancellationToken = default)
            => Task.FromResult<TraceView?>(null);
        public Task<CorrelationView?> CorrelationAsync(string correlationId, CancellationToken cancellationToken = default)
            => Task.FromResult<CorrelationView?>(null);
    }

    private static AwsLambdaBenzeneTestHost CreateHost()
    {
        return new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services.UsingBenzene(x =>
            {
                x.AddBenzene();
                x.AddMessageHandlers(typeof(AwsMeshFleetEndpointTest).Assembly); // outer router (no mesh:query:* here)
                x.AddHttpMessageHandlers();
                x.AddSingleton<IMeshFleetReadModel>(new FakeReadModel());
            }))
            .Configure(app => app
                .UseApiGateway(http => http
                    // Mirrors the AwsMesh mesh Lambda: the mesh UI with its live Fleet plane pointed at the
                    // envelope endpoint, the envelope endpoint routing the query handlers over the composite
                    // read model, then the fall-through handler router.
                    .UseMeshUi("/mesh-ui", "manifest.json", "/benzene/invoke")
                    .UseBenzeneMessage(new BenzeneMessageHttpOptions { Path = "/benzene/invoke" },
                        fleet => fleet.UseMessageHandlers(MeshCollectorHandlers.Queries))
                    .UseMessageHandlers(typeof(AwsMeshFleetEndpointTest).Assembly)))
            .BuildHost();
    }

    [Fact]
    public async Task PostFleetQuery_ToEnvelopeEndpoint_Returns200()
    {
        var host = CreateHost();

        var response = await host.SendApiGatewayAsync(HttpBuilder
            .Create("POST", "/benzene/invoke", new
            {
                topic = "mesh:query:fleet",
                headers = new System.Collections.Generic.Dictionary<string, string>(),
                body = "{}"
            }));

        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.Contains("\"statusCode\":\"ok\"", response.Body);
    }

    [Fact]
    public async Task GetMeshUiPage_IsServed_WithFleetPlaneWired()
    {
        var host = CreateHost();

        var response = await host.SendApiGatewayAsync(HttpBuilder.Create("GET", "/mesh-ui"));

        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        // The catalog page carries the injected envelope URL its live Fleet plane feature-detects and polls.
        Assert.Contains("data-fleet-url=\"/benzene/invoke\"", response.Body);
    }
}
