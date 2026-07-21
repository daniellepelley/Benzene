using Benzene.Abstractions.Hosting;
using Benzene.AspNet.Core;
using Benzene.CloudService;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Diagnostics;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Benzene.OpenTelemetry;
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

        services.UsingBenzene(x => x
            .AddBenzene()
            // Name the service in its own derived spec (the benzene spec's title = IApplicationInfo.Name).
            // Without this it defaults to BlankApplicationInfo and the spec/Spec UI show no service name.
            // Matches the name the mesh discovers this service under (its Kubernetes Service name).
            .SetApplicationInfo(ServiceName, "1.0.0", $"{ServiceName} service")
            .AddDiagnostics()
            .AddMessageHandlers(Domain.HandlersFor(ServiceName))
            .AddHttpMessageHandlers());
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        var name = ServiceName;
        IHealthCheck[] healthChecks = { new ServiceHealthCheck(name) };

        app.UseHttp(asp => asp
            .UseW3CTraceContext()
            .UseBenzeneEnrichment()
            .UseBenzeneMetrics()
            .UseSpecUi("/benzene/spec-ui", "/benzene/spec?type=benzene")
            .UseBenzeneCloudService($"{name}-api", cloud => cloud
                .WithServiceVersion("1.0.0")
                .WithInstanceId(name)
                .WithPlacement("kubernetes", "in-cluster")
                .WithHealthChecks(healthChecks)
                .WithHandlers(Domain.HandlersFor(name))));
    }
}
