using Benzene.Abstractions.MessageHandlers;
using Benzene.AspNet.Core;
using Benzene.CloudService;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Versioning;
using Benzene.Core.Versioning.Schemas;
using Benzene.Examples.Mesh.PaymentsService.HealthChecks;
using Benzene.Examples.Mesh.PaymentsService.Handlers;
using Benzene.Examples.Mesh.PaymentsService.Model;
using Benzene.Http;
using Benzene.Http.Routing;
using Benzene.Microsoft.Dependencies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using V1 = Benzene.Examples.Mesh.PaymentsService.Model.V1;
using V2 = Benzene.Examples.Mesh.PaymentsService.Model.V2;

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

        // Payload versioning demo (docs/specification/versioning.md): the single V2 payments:get handler
        // serves v1 and v2 clients. AddHttpVersioning exposes /v1/payments/{id} and /v2/payments/{id}
        // (plus the unversioned /payments/{id}, which resolves to latest); the payload caster downcasts the
        // handler's V2 response to V1 for a /v1 request (currency dropped). UsePayloadVersionCasting must be
        // the last mapper registration, so it wraps the framework defaults - hence inside UsingBenzene here.
        services.UsingBenzene(x => x
            .AddHttpVersioning()
            .RegisterSchemaCastDefinitions(builder => builder
                .Add<V2.PaymentDto, V1.PaymentDto>("payments:get", "2", "1"))
            .RegisterPayloadSchemaVersions(new[]
            {
                new PayloadSchemaVersions
                {
                    Topic = "payments:get",
                    FromSchemas = new[] { "2" },      // the handler's canonical version (what it produces)
                    ToSchemas = new[] { "1", "2" },   // the wire versions a client can ask for
                },
            })
            .UsePayloadVersionCasting<AspNetContext>());
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
