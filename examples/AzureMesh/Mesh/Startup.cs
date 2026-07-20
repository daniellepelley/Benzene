using Benzene.Abstractions.Hosting;
using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Diagnostics;
using Benzene.Http;
using Benzene.Http.Cors;
using Benzene.Mesh.Azure.Blob;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Discovery.Azure;
using Benzene.Mesh.Ui;
using Benzene.Microsoft.Dependencies;
using Benzene.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Benzene.Examples.AzureMesh.Mesh;

/// <summary>
/// The mesh service, hosted as an ASP.NET Core container on Azure (App Service / Container App). It
/// discovers the benzene-tagged Azure App Services (via Azure Resource Manager, using the app's
/// managed identity), writes the discovered registry + catalog to Blob Storage, and interrogates each
/// service over HTTPS — then serves the Mesh UI and catalog artifacts. A background service re-runs the
/// pass on an interval; <c>POST /mesh/refresh</c> triggers one on demand.
/// </summary>
public class Startup : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var blobServiceUri = Environment.GetEnvironmentVariable("MESH_BLOB_URI")
                             ?? throw new InvalidOperationException("MESH_BLOB_URI is required (e.g. https://acct.blob.core.windows.net).");
        var container = Environment.GetEnvironmentVariable("MESH_BLOB_CONTAINER") ?? "mesh";
        var prefix = Environment.GetEnvironmentVariable("MESH_BLOB_PREFIX") ?? "";

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
            // Discovery starts with an empty registry — discovery replaces it at runtime; artifacts live in Blob Storage.
            .AddMeshAggregatorWithBlob(new MeshServiceRegistry(Array.Empty<MeshServiceRegistryEntry>()),
                new Uri(blobServiceUri), container, prefix)
            .AddMeshAzureDiscovery());

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
            // Allow the AsyncAPI Studio deep-link to fetch asyncapi.json cross-origin. Uses
            // Benzene's own CORS support (Benzene.Http.Cors.CorsSettings); "*" would open it to
            // any origin, but scoping to Studio's origin keeps the example tight.
            .UseMeshArtifacts(new CorsSettings { AllowedDomains = new[] { "https://studio.asyncapi.com" } })
            .UseMessageHandlers(typeof(Startup).Assembly));
    }
}
