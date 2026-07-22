using Benzene.Abstractions.Hosting;
using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.Timer;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Http;
using Benzene.Http.Cors;
using Benzene.Mesh.Azure.Blob;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Discovery.Azure;
using Benzene.Mesh.Ui;
using Benzene.Mesh.Usage.ApplicationInsights;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AzureFunctionsMesh.Mesh;

/// <summary>
/// The mesh, hosted purely as <b>Azure Functions</b> (isolated worker). A catch-all HTTP trigger serves
/// the Mesh UI (<c>/mesh-ui</c>) and the catalog artifacts read from Blob Storage; a timer trigger runs
/// the discovery + aggregation pass on a schedule (the Functions replacement for the Web App's
/// <c>BackgroundService</c>, which a Consumption-plan Function can't rely on). Discovery finds the
/// benzene-tagged Azure sites (Web Apps and Function Apps alike, since both are
/// <c>Microsoft.Web/sites</c>) via Azure Resource Manager using the app's managed identity, and writes
/// the catalog to Blob Storage. <c>POST /mesh/refresh</c> triggers a pass on demand.
/// </summary>
public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var blobServiceUri = Environment.GetEnvironmentVariable("MESH_BLOB_URI")
                             ?? throw new InvalidOperationException("MESH_BLOB_URI is required (e.g. https://acct.blob.core.windows.net).");
        var container = Environment.GetEnvironmentVariable("MESH_BLOB_CONTAINER") ?? "mesh";
        var prefix = Environment.GetEnvironmentVariable("MESH_BLOB_PREFIX") ?? "";

        services.UsingBenzene(benzene =>
        {
            benzene
                .AddBenzene()
                .AddDiagnostics()
                .AddMessageHandlers(typeof(StartUp).Assembly)
                .AddHttpMessageHandlers()
                // Discovery starts with an empty registry — discovery replaces it at runtime; artifacts live in Blob Storage.
                .AddMeshAggregatorWithBlob(new MeshServiceRegistry(Array.Empty<MeshServiceRegistryEntry>()),
                    new Uri(blobServiceUri), container, prefix)
                // Scope discovery to this deployment's subscription + resource group so a subscription-scoped
                // Reader identity doesn't discover every benzene-tagged site. Both optional (unset falls back
                // to the credential's default subscription, whole-subscription sweep).
                .AddMeshAzureDiscovery(
                    subscriptionId: Environment.GetEnvironmentVariable("MESH_SUBSCRIPTION_ID"),
                    resourceGroup: Environment.GetEnvironmentVariable("MESH_RESOURCE_GROUP"));

            // Usage feed: read the benzene.messages.processed counter (exported to Application Insights by
            // each service's Azure Monitor exporter) back from the Log Analytics workspace as per-topic
            // request counts over a window, merged into usage.json each run. Only wired when the workspace
            // id is configured (so the example still runs without App Insights). Window: MESH_USAGE_WINDOW_HOURS.
            var workspaceId = Environment.GetEnvironmentVariable("MESH_LOG_ANALYTICS_WORKSPACE_ID");
            if (!string.IsNullOrEmpty(workspaceId))
            {
                var usageWindowHours = double.TryParse(
                    Environment.GetEnvironmentVariable("MESH_USAGE_WINDOW_HOURS"), out var hours) ? hours : 24;
                benzene.AddApplicationInsightsUsage(
                    new ApplicationInsightsUsageOptions(workspaceId, TimeSpan.FromHours(usageWindowHours)));
            }
        });

        services.AddSingleton<MeshAggregationService>();
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        // The public HTTP surface: the Mesh UI and the catalog artifacts, plus the on-demand refresh handler.
        app.UseHttp(http => http
            .UseBenzeneEnrichment()
            .UseMeshUi("/mesh-ui", "manifest.json")
            // The mesh-hosted per-service Spec UI (mesh-ui's "spec" link). Renders each service's spec
            // from the same-origin services/{name}.json snapshot, so a service only serves JSON.
            .UseMeshSpecUi("/mesh-spec-ui.html", "manifest.json")
            // Allow the AsyncAPI Studio deep-link to fetch asyncapi.json cross-origin (Benzene's own CORS).
            .UseMeshArtifacts(new CorsSettings { AllowedDomains = new[] { "https://studio.asyncapi.com" } })
            .UseMessageHandlers(typeof(StartUp).Assembly));

        // The scheduled discovery + aggregation pass. The timer tick resolves the (singleton) aggregation
        // service and runs one pass; the single-flight gate inside coordinates with /mesh/refresh.
        app.UseTimerTrigger(timer => timer
            .Use(resolver => new FuncWrapperMiddleware<TimerContext>("aggregate", async (context, next) =>
            {
                await resolver.GetService<MeshAggregationService>().RunAsync();
                await next();
            })));
    }
}
