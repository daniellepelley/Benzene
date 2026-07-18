using Benzene.Abstractions.Hosting;
using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Http;
using Benzene.Mesh.Azure.Blob;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Discovery.Azure;
using Benzene.Mesh.Ui;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.UsingBenzene(benzene => benzene
            .AddBenzene()
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
            .UseMeshUi("/mesh-ui", "manifest.json")
            .UseMeshArtifacts()
            .UseMessageHandlers(typeof(Startup).Assembly));
    }
}
