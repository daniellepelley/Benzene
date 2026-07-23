using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.AspNet.Core;
using Benzene.Clients.GoogleCloud.PubSub;
using Benzene.CloudService;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Diagnostics;
using Benzene.GoogleCloud.Functions.PubSub;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.GoogleCloudMesh.Shared;

/// <summary>
/// The shared wiring for a Google Cloud Mesh service. Because a Cloud Functions Gen2 function has
/// exactly one trigger, each service is deployed as TWO functions sharing this one <c>Startup</c>: an
/// HTTP function (the Cloud Service Profile the mesh polls) and — for consumers — a Pub/Sub CloudEvent
/// function. <c>Configure</c> calls both <c>UseHttp</c> and <c>UsePubSub</c>; each is a no-op on the
/// host it doesn't apply to (<c>UseHttp</c> off an ASP builder, <c>UsePubSub</c> off the Pub/Sub
/// builder), so the same object runs unchanged on both hosts.
/// </summary>
public static class MeshServiceWiring
{
    /// <summary>
    /// Registers the baseline, the domain handlers, and (via <paramref name="configureBenzene"/>) any
    /// outbound Pub/Sub routing the service publishes over. Each outbound send both declares the topic
    /// in the spec's <c>events</c> (→ the mesh topology) and wires the runtime Pub/Sub route.
    /// </summary>
    public static void ConfigureServices(IServiceCollection services, string serviceName, Type[] handlers,
        Action<IBenzeneServiceContainer>? configureBenzene = null)
    {
        services.AddLogging();

        services.UsingBenzene(x =>
        {
            x.AddBenzene()
                .SetApplicationInfo(serviceName, "1.0.0", $"{serviceName} service")
                .AddDiagnostics()
                .AddMessageHandlers(handlers)
                .AddHttpMessageHandlers();
            configureBenzene?.Invoke(x);
        });
    }

    /// <summary>
    /// Wires the HTTP Cloud Service Profile (served by the HTTP function, polled by the mesh) and the
    /// Pub/Sub ingress (served by the Pub/Sub function). One or both is active depending on which host
    /// this <c>Startup</c> is running under.
    /// </summary>
    public static void Configure(IBenzeneApplicationBuilder app, string serviceName, Type[] handlers,
        IHealthCheck[] healthChecks)
    {
        var region = Environment.GetEnvironmentVariable("REGION") ?? "local";

        // HTTP surface: the full Cloud Service Profile (/benzene/spec, /health, /invoke). Active only on
        // the HTTP function (no-op on the Pub/Sub function).
        app.UseHttp(http => http
            .UseBenzeneEnrichment()
            .UseBenzeneCloudService($"{serviceName}-api", cloud => cloud
                .WithServiceVersion("1.0.0")
                .WithInstanceId(serviceName)
                .WithPlacement("google-cloud-functions", region)
                .WithHealthChecks(healthChecks)
                .WithHandlers(handlers)));

        // Pub/Sub ingress: the same domain handlers, routed by the "topic" message attribute. Active
        // only on the Pub/Sub function (no-op on the HTTP function).
        app.UsePubSub(pipeline => pipeline
            .UseBenzeneEnrichment()
            .UseMessageHandlers(handlers));
    }

    /// <summary>
    /// Registers a Pub/Sub publisher (Application Default Credentials) for a service that publishes
    /// events — lazy so a service with no credentials still starts locally.
    /// </summary>
    public static void AddPubSubPublisher(this IBenzeneServiceContainer services)
        => services.AddSingleton(_ => PublisherServiceApiClient.Create());
}
