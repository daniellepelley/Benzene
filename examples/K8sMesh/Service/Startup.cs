using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.AspNet.Core;
using Benzene.Client.Http;
using Benzene.Clients;
using Benzene.CloudService;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Versioning;
using Benzene.Core.Versioning.Schemas;
using Benzene.Diagnostics;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Http.BenzeneMessage;
using Benzene.Microsoft.Dependencies;
using Benzene.OpenTelemetry;
using Benzene.ResponseEvents;
using Benzene.Spec.Ui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Benzene.Examples.K8sMesh.Service;

/// <summary>
/// One Benzene Cloud Service, hosted as an ASP.NET Core container. The domain it serves
/// (orders/payments/shipping) is chosen at startup by the <c>MESH_SERVICE</c> env var, so a single
/// image is deployed three times as three labelled Kubernetes Services. Exposes the full Cloud Service
/// Profile over HTTP — <c>/benzene/spec</c>, <c>/benzene/health</c>, <c>/benzene/invoke</c>,
/// <c>/benzene/spec-ui</c> — which is exactly how the mesh interrogates it (plain HTTP, in-cluster).
/// </summary>
public class Startup : BenzeneStartUp
{
    private static string ServiceName => Environment.GetEnvironmentVariable("MESH_SERVICE") ?? "orders";

    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Full OpenTelemetry: Benzene traces + metrics. The OTLP exporter is only attached when
        // OTEL_EXPORTER_OTLP_ENDPOINT is set — otherwise the instrumentation is armed but nothing tries
        // to reach a collector, so there are no connection-refused errors in the logs without one.
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService($"{ServiceName}-api"))
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

        services.UsingBenzene(x =>
        {
            x.AddBenzene()
                // Name the service in its own derived spec (the benzene spec's title = IApplicationInfo.Name).
                // Without this it defaults to BlankApplicationInfo and the spec/Spec UI show no service name.
                // Matches the name the mesh discovers this service under (its Kubernetes Service name).
                .SetApplicationInfo(ServiceName, "1.0.0", $"{ServiceName} service")
                .AddDiagnostics()
                .AddMessageHandlers(Domain.HandlersFor(ServiceName))
                .AddHttpMessageHandlers();

            // Outbound: chain to the next service over the BenzeneMessage envelope endpoint. The target is
            // the downstream service's in-cluster URL (e.g. http://payments/benzene-message), supplied by the
            // DOWNSTREAM_MSG_URL env var — see k8s/services.yaml. HttpBenzeneMessageClient POSTs the
            // { topic, headers, body } envelope there and also auto-wires a non-destructive reachability check
            // (a healthcheck-topic POST) onto the deep healthcheck layer. When no downstream is wired (the
            // terminal shipping service, or a standalone run) a null client stands in so the handlers resolve.
            var downstreamUrl = Environment.GetEnvironmentVariable("DOWNSTREAM_MSG_URL");
            if (!string.IsNullOrEmpty(downstreamUrl))
            {
                x.AddSingleton(_ => new HttpClient());
                x.AddHttpBenzeneMessageClient(downstreamUrl);
            }
            else
            {
                x.AddScoped<IBenzeneMessageClient>(_ => new NullBenzeneMessageClient());
            }

            ConfigureVersioning(x);
        });
    }

    // Message versioning across the payment:take hop (docs/specification/versioning.md):
    //  - payments-api runs a single V2 handler and registers the V1->V2 upcaster + payload casting, so a v1
    //    payload from orders-api is upcast (currency seeded) before the handler sees it. The version travels
    //    in the benzene-version envelope header (orders sends SendMessageAsync(..., version: "1")).
    //  - orders-api declares it produces payment:take@1 (spec events), so the mesh's version compatibility
    //    view surfaces the producer-v1 / consumer-v2 skew the upcaster bridges.
    private static void ConfigureVersioning(IBenzeneServiceContainer x)
    {
        if (ServiceName == "payments")
        {
            x.RegisterSchemaCastDefinitions(builder => builder
                    .Add<Model.V1.TakePaymentRequest, Model.V2.TakePaymentRequest>("payment:take", "1", "2",
                        f => f.RegisterInitValue(r => r.Currency, "GBP")))
                .RegisterPayloadSchemaVersions(new[]
                {
                    new PayloadSchemaVersions
                    {
                        Topic = "payment:take",
                        FromSchemas = new[] { "1", "2" }, // versions that may arrive on the wire
                        ToSchemas = new[] { "2" },        // the single handler only understands v2
                    },
                })
                .UsePayloadVersionCasting<BenzeneMessageContext>();
        }
        else if (ServiceName == "orders")
        {
            x.AddResponseEventDeclarations(
                new ResponseEventDefinition(new Benzene.Core.Messages.Topic("payment:take", "1"),
                    typeof(Model.V1.TakePaymentRequest)));
        }
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        var name = ServiceName;
        IHealthCheck[] healthChecks = { new ServiceHealthCheck(name) };

        // When a collector is reachable (MESH_COLLECTOR_ENVELOPE_URL, set by the k8s/compose manifests
        // to the mesh's /benzene/invoke), the Cloud Service reports to it: register + heartbeat +
        // per-invocation traces. This is what populates the mesh's live Fleet view. It's best-effort -
        // an unreachable collector reduces the mesh, never the service - so it stays unset-safe (the
        // service runs identically with no collector, e.g. the aggregator-only setups).
        var collectorEnvelopeUrl = Environment.GetEnvironmentVariable("MESH_COLLECTOR_ENVELOPE_URL");

        app.UseHttp(asp => asp
            .UseW3CTraceContext()
            .UseBenzeneEnrichment()
            .UseBenzeneMetrics()
            // Receive lightweight BenzeneMessages from other services: a POST of a { topic, headers, body }
            // envelope to /benzene-message is routed to this service's handlers by the envelope's topic — the
            // ingress half of the orders → payments → shipping chain (HttpBenzeneMessageClient is the egress
            // half). The healthcheck topic is routed too, so the callers' auto-wired reachability probe gets a
            // 200. This is the same server endpoint the AWS Lambda invoke path exposes, over HTTP.
            .UseBenzeneMessage(bm => bm
                .UseHealthCheck("healthcheck", healthChecks)
                .UseMessageHandlers(_ => { }))
            .UseSpecUi("/benzene/spec-ui", "/benzene/spec?type=benzene")
            .UseBenzeneCloudService($"{name}-api", cloud =>
            {
                cloud
                    .WithServiceVersion("1.0.0")
                    .WithInstanceId(name)
                    .WithPlacement("kubernetes", "in-cluster")
                    .WithHealthChecks(healthChecks)
                    .WithHandlers(Domain.HandlersFor(name));
                if (!string.IsNullOrWhiteSpace(collectorEnvelopeUrl))
                {
                    cloud.WithCollector(collectorEnvelopeUrl);
                }
            }));
    }
}
