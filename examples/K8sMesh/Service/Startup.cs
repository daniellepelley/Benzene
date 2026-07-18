using Benzene.Abstractions.Hosting;
using Benzene.AspNet.Core;
using Benzene.CloudService;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Benzene.Spec.Ui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(Domain.HandlersFor(ServiceName))
            .AddHttpMessageHandlers());
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        var name = ServiceName;
        IHealthCheck[] healthChecks = { new ServiceHealthCheck(name) };

        app.UseHttp(asp => asp
            .UseSpecUi("/benzene/spec-ui", "/benzene/spec?type=benzene")
            .UseBenzeneCloudService($"{name}-api", cloud => cloud
                .WithServiceVersion("1.0.0")
                .WithInstanceId(name)
                .WithPlacement("kubernetes", "in-cluster")
                .WithHealthChecks(healthChecks)
                .WithHandlers(Domain.HandlersFor(name))));
    }
}
