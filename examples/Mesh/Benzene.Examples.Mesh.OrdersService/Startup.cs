using Benzene.AspNet.Core;
using Benzene.CloudService;
using Benzene.Examples.Mesh.OrdersService.HealthChecks;
using Benzene.Microsoft.Dependencies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.UsingBenzene();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        // Benzene Cloud Service setup (docs/specification/cloud-service-profile.md): this one call
        // replaces the old hand-wired /benzene/invoke branch + StartAnnouncing() (MeshHost.cs,
        // deleted) with the wire-envelope endpoint, health checks (reserved topic + HTTP), the
        // derived spec, message handlers via the registry, and the mesh service-side feeds
        // (descriptor, registration, heartbeats, trace) - all pre-wired in the right order. Handler
        // discovery stays attribute-based ([Message]/[HttpEndpoint] on GetOrdersMessageHandler /
        // CheckoutOrderMessageHandler), same idiom as before. Spec/health stay at this demo's
        // pre-existing /spec and /healthcheck paths (relocated from the /benzene/ defaults) so the
        // aggregator's polling and run.sh keep working unchanged - the profile report honestly
        // flags R7 for that deliberate relocation.
        app.UseBenzene(benzene => benzene
            .UseHttp(asp => asp
                .UseBenzeneCloudService("orders-api", cloud => cloud
                    .WithServiceVersion("1.0.0")
                    .WithInstanceId("orders-api-1")
                    .WithHealthChecks(new OrdersDatabaseHealthCheck(), new OrdersCacheHealthCheck(), new OrdersQueueHealthCheck())
                    .WithCollector(Environment.GetEnvironmentVariable("MESH_COLLECTOR_ENVELOPE_URL")
                        ?? "http://localhost:5300/benzene/invoke")
                    .WithSpecPath("/spec")
                    .WithHealthPath("/healthcheck")
                )
            )
        );

        app.UseEndpoints(endpoints => { });
    }
}
