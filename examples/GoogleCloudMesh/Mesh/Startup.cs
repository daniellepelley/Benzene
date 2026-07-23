using Benzene.Abstractions.Hosting;
using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Diagnostics;
using Benzene.Http;
using Benzene.Http.Cors;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.GoogleCloud.Storage;
using Benzene.Mesh.Ui;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.GoogleCloudMesh.Mesh;

/// <summary>
/// The mesh aggregator, hosted as a Cloud Functions Gen2 HTTP function. It polls each service's HTTP
/// Cloud Service Profile (a static registry from env), writes the catalog to Google Cloud Storage, and
/// serves the mesh UI + artifacts. Aggregation is driven on demand by <c>POST /mesh/refresh</c> (Cloud
/// Scheduler hits it periodically, since Cloud Functions has no timer trigger).
/// </summary>
public class Startup : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging();

        var bucket = Environment.GetEnvironmentVariable("MESH_BUCKET")
                     ?? throw new InvalidOperationException("MESH_BUCKET is required.");
        var prefix = Environment.GetEnvironmentVariable("MESH_PREFIX") ?? "";

        services.UsingBenzene(x => x
            .AddBenzene()
            .AddDiagnostics()
            .AddMessageHandlers(typeof(Startup).Assembly)
            .AddHttpMessageHandlers()
            // Discovery is supplied statically (MeshRegistry.FromEnvironment); catalog persists to GCS.
            .AddMeshAggregatorWithGcs(new MeshServiceRegistry(Array.Empty<MeshServiceRegistryEntry>()), bucket, prefix));

        services.AddSingleton<MeshAggregationService>();
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseHttp(http => http
            .UseBenzeneEnrichment()
            .UseMeshUi("/mesh-ui", "manifest.json")
            .UseMeshSpecUi("/mesh-spec-ui.html", "manifest.json")
            .UseMeshArtifacts(new CorsSettings { AllowedDomains = new[] { "https://studio.asyncapi.com" } })
            .UseMessageHandlers(typeof(Startup).Assembly));
    }
}
