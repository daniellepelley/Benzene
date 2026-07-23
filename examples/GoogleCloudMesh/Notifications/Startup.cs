using Benzene.Abstractions.Hosting;
using Benzene.Examples.GoogleCloudMesh.Shared;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.GoogleCloudMesh.Notifications;

/// <summary>notifications-api: a pure consumer of order:placed, payment:captured, shipment:dispatched.</summary>
public class Startup : BenzeneStartUp
{
    private static readonly Type[] Handlers =
    {
        typeof(OrderPlacedHandler), typeof(PaymentCapturedHandler), typeof(ShipmentDispatchedHandler)
    };

    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => MeshServiceWiring.ConfigureServices(services, "notifications", Handlers);

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks = { new ServiceHealthCheck("notifications") };
        MeshServiceWiring.Configure(app, "notifications", Handlers, healthChecks);
    }
}
