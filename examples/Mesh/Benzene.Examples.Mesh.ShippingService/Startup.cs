using Benzene.AspNet.Core;
using Benzene.CloudService;
using Benzene.Examples.Mesh.ShippingService.HealthChecks;
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

        // Benzene Cloud Service setup (docs/specification/cloud-service-profile.md) - see
        // OrdersService/Startup.cs for the full before/after story this replaces (MeshHost.cs,
        // deleted). Not started by default (run.sh leaves shipping-api down), so the Fleet view
        // demonstrates its absence honestly: no row until it starts and registers. Spec/health
        // stay at the demo's pre-existing /spec and /healthcheck paths (relocated from the
        // /benzene/ defaults), flagged as R7 in the report.
        app.UseBenzene(benzene => benzene
            .UseHttp(asp => asp
                .UseBenzeneCloudService("shipping-api", cloud => cloud
                    .WithServiceVersion("1.0.0")
                    .WithInstanceId("shipping-api-1")
                    .WithHealthChecks(new ShippingCarrierApiHealthCheck(), new ShippingQueueHealthCheck())
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
