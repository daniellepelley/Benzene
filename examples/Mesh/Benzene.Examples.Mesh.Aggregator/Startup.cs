using Benzene.AspNet.Core;
using Benzene.Examples.Mesh.Shared;
using Benzene.Core.MessageHandlers;
using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Collector;
using Benzene.Mesh.Tracing.Tempo;
using Benzene.Mesh.Ui;
using Benzene.Microsoft.Dependencies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

public class Startup
{
    private static readonly string ArtifactDirectory = Path.Combine(AppContext.BaseDirectory, "mesh-artifacts");

    // The live spec collector (Benzene.Mesh.Collector) behind a wire-envelope endpoint: services
    // register, heartbeat, and push traces here, and the Fleet view below renders what it derives.
    private static readonly MeshCollectorStore CollectorStore = new();
    private static readonly EnvelopeHost Collector = new(
        MeshCollectorHandlers.All,
        configureServices: services => services.AddSingleton(CollectorStore));

    private static readonly MeshServiceRegistry Registry = new(new[]
    {
        new MeshServiceRegistryEntry("orders-api", "http://localhost:5310/spec?type=benzene", "http://localhost:5310/healthcheck"),
        new MeshServiceRegistryEntry("payments-api", "http://localhost:5311/spec?type=benzene", "http://localhost:5311/healthcheck"),
        new MeshServiceRegistryEntry("shipping-api", "http://localhost:5312/spec?type=benzene", "http://localhost:5312/healthcheck"),
    });

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        Directory.CreateDirectory(ArtifactDirectory);
        services.UsingBenzene(x => x
            .AddMeshAggregator(Registry, ArtifactDirectory)
            // Queries the fake Prometheus endpoint below instead of a real Tempo/Prometheus stack,
            // so this example stays self-contained (no Docker, no network egress) - see FakePrometheus.cs.
            .AddTempoTopology(new TempoTopologyOptions("http://localhost:5300/fake-prometheus/api/v1/query")));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // The collector's wire-envelope endpoint - branched before UseBenzene so the HTTP
        // pipeline never sees it (the Fleet view at /fleet-ui polls it).
        app.Map("/invoke", branch => branch.Run(Collector.HandleAsync));

        app.UseRouting();

        // Serves the aggregator's own generated manifest.json/services/*.json - the real,
        // continuously-refreshed data behind the dashboard below.
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(ArtifactDirectory),
            RequestPath = "/artifacts",
        });

        app.UseBenzene(benzene => benzene
            .UseHttp(asp => asp
                .UseMeshUi(path: "/mesh-ui", manifestUrl: "/artifacts/manifest.json")
                // The NEW Fleet view: live derived fleet from the collector, polled through /invoke.
                .UseMeshFleetUi(path: "/fleet-ui", envelopeUrl: "/invoke")
                .UseMessageHandlers()
            )
        );

        // A fake Prometheus-compatible endpoint standing in for a real Tempo/Prometheus stack -
        // see FakePrometheus.cs.
        app.UseEndpoints(endpoints => { endpoints.MapGet("/fake-prometheus/api/v1/query", FakePrometheus.Handle); });
    }
}
