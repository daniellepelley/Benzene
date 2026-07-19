using Benzene.Abstractions.Hosting;
using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Diagnostics;
using Benzene.Http;
using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Discovery.Kubernetes;
using Benzene.Mesh.Ui;
using Benzene.Microsoft.Dependencies;
using Benzene.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Benzene.Examples.K8sMesh.Mesh;

/// <summary>
/// The mesh service, hosted as an ASP.NET Core container in the cluster. It discovers the
/// benzene-labelled Kubernetes Services (via the Kubernetes API), writes the discovered registry, and
/// interrogates each over plain in-cluster HTTP — then serves the Mesh UI and catalog artifacts. A
/// background service re-runs the pass on an interval; <c>POST /mesh/refresh</c> triggers one on demand.
/// The catalog is stored on the pod's own volume (single writer + reader), so no blob store is needed.
/// </summary>
public class Startup : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var artifactDir = Environment.GetEnvironmentVariable("MESH_ARTIFACT_DIR") ?? "/artifacts";

        // The OTLP exporter is only attached when OTEL_EXPORTER_OTLP_ENDPOINT is set — the
        // instrumentation is armed either way, so there are no connection-refused errors without one.
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("benzene-mesh"))
            .WithTracing(tracing =>
            {
                tracing.SetSampler(new AlwaysOnSampler()).AddBenzeneInstrumentation();
                if (!string.IsNullOrEmpty(otlpEndpoint)) tracing.AddOtlpExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddBenzeneInstrumentation();
                if (!string.IsNullOrEmpty(otlpEndpoint)) metrics.AddOtlpExporter();
            });

        services.UsingBenzene(benzene => benzene
            .AddBenzene()
            .AddDiagnostics()
            .AddMessageHandlers(typeof(Startup).Assembly)
            .AddHttpMessageHandlers()
            // Discovery starts with an empty registry — discovery replaces it at runtime.
            .AddMeshAggregator(new MeshServiceRegistry(Array.Empty<MeshServiceRegistryEntry>()), artifactDir)
            .AddMeshKubernetesDiscovery());

        services.AddSingleton<MeshAggregationService>();
        services.AddHostedService<MeshAggregationBackgroundService>();
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        // Scope handler discovery to this assembly so Benzene.Mesh.Aggregator's own
        // MeshAggregateMessageHandler (also [Message("mesh:aggregate")]) isn't discovered too.
        app.UseHttp(asp => asp
            .UseW3CTraceContext()
            .UseBenzeneEnrichment()
            .UseBenzeneMetrics()
            .UseMeshUi("/mesh-ui", "manifest.json")
            .UseMeshArtifacts()
            .UseMessageHandlers(typeof(Startup).Assembly));
    }
}
