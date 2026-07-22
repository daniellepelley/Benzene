using Benzene.Abstractions.Hosting;
using Benzene.Azure.Function.AspNet;
using Benzene.CloudService;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Diagnostics;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AzureFunctionsMesh.Service;

/// <summary>
/// One Benzene Cloud Service, hosted as an <b>Azure Function</b> (isolated worker, HTTP trigger). The
/// domain it serves (orders/payments/shipping) is chosen at startup by the <c>MESH_SERVICE</c> env var,
/// so a single deployable is published three times as three tagged Function Apps. It exposes the
/// Cloud Service Profile over HTTP — <c>/benzene/spec</c>, <c>/benzene/health</c>, <c>/benzene/invoke</c>
/// — which is exactly how the mesh interrogates it (plain HTTPS). Note it serves <b>no HTML</b> of its
/// own: the Cloud Service contract is JSON only (spec/health/invoke), and the spec is rendered by the
/// mesh-hosted Spec UI (<c>UseMeshSpecUi</c>), not by the service. The catch-all
/// <see cref="HttpFunction"/> forwards every request into this pipeline; <c>host.json</c> clears the
/// <c>/api</c> route prefix so those paths sit at the root the mesh's default discovery URLs expect.
/// </summary>
public class StartUp : BenzeneStartUp
{
    private static string ServiceName => Environment.GetEnvironmentVariable("MESH_SERVICE") ?? "orders";

    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.UsingBenzene(x => x
            .AddBenzene()
            // Name the service in its own derived spec (the benzene spec's title = IApplicationInfo.Name);
            // without this it falls back to a blank name. Matches the name the mesh discovers it under.
            .SetApplicationInfo(ServiceName, "1.0.0", $"{ServiceName} service")
            .AddDiagnostics()
            .AddMessageHandlers(Domain.HandlersFor(ServiceName))
            .AddHttpMessageHandlers());
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        var name = ServiceName;
        var region = Environment.GetEnvironmentVariable("REGION_NAME") ?? "local";
        IHealthCheck[] healthChecks = { new ServiceHealthCheck(name) };

        // No UseSpecUi here on purpose: a Cloud Service serves only JSON (spec/health/invoke); it does
        // not host any HTML. The browsable spec view is served by the mesh instead (UseMeshSpecUi),
        // which renders this service's captured spec from the mesh's own same-origin artifacts.
        app.UseHttp(http => http
            .UseBenzeneEnrichment()
            .UseBenzeneCloudService($"{name}-api", cloud => cloud
                .WithServiceVersion("1.0.0")
                .WithInstanceId(name)
                .WithPlacement("azure-functions", region)
                .WithHealthChecks(healthChecks)
                .WithHandlers(Domain.HandlersFor(name))));
    }
}
