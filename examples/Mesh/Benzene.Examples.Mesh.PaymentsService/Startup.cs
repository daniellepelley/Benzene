using Benzene.Abstractions.MessageHandlers;
using Benzene.AspNet.Core;
using Benzene.CloudService;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.Mesh.PaymentsService.HealthChecks;
using Benzene.Examples.Mesh.PaymentsService.Handlers;
using Benzene.Examples.Mesh.PaymentsService.Model;
using Benzene.Http.Routing;
using Benzene.Microsoft.Dependencies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        if (Environment.GetEnvironmentVariable("DEMO_ADD_ENDPOINT") == "true")
        {
            // Not attribute-discoverable by design (see GetPaymentRefundsMessageHandler's own
            // doc comment) - manually registered here so it can be toggled at runtime to drive
            // the contract-drift demo. UseBenzeneCloudService's handler registry (below) picks
            // this DI registration up the same way it picks up attribute-discovered handlers.
            services.AddSingleton<IMessageHandlerDefinition>(_ =>
                MessageHandlerDefinition.CreateInstance("payments:get-refunds", "", typeof(GetPaymentMessage),
                    typeof(RefundDto[]), typeof(GetPaymentRefundsMessageHandler)));
            services.AddScoped<GetPaymentRefundsMessageHandler>();
            services.AddSingleton<IHttpEndpointDefinition>(_ =>
                new HttpEndpointDefinition("get", "/payments/{id}/refunds", "payments:get-refunds"));
        }

        services.UsingBenzene();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        // Benzene Cloud Service setup (docs/specification/cloud-service-profile.md) - see
        // OrdersService/Startup.cs for the full before/after story this replaces (MeshHost.cs,
        // deleted). DEMO_ADD_ENDPOINT changes the derived descriptor (and so its hash) exactly as
        // it changes the OpenAPI spec - the same contract-drift story the aggregator demo tells,
        // visible on the Fleet view as a hash mismatch until the restarted instance re-registers.
        // DEMO_PAYMENTS_HEALTHY drives the health check below, so the Fleet view mirrors the
        // dashboard's unhealthy badge. Spec/health stay at the demo's pre-existing /spec and
        // /healthcheck paths (relocated from the /benzene/ defaults), flagged as R7 in the report.
        app.UseBenzene(benzene => benzene
            .UseHttp(asp => asp
                .UseBenzeneCloudService("payments-api", cloud => cloud
                    .WithServiceVersion("1.0.0")
                    .WithInstanceId("payments-api-1")
                    .WithHealthChecks(
                        new PaymentsGatewayHealthCheck(), new PaymentsDatabaseHealthCheck(), new FraudEngineHealthCheck())
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
