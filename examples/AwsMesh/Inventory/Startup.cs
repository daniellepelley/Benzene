using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.Core;
using Benzene.Examples.AwsMesh.Inventory.Handlers;
using Benzene.Examples.AwsMesh.Inventory.HealthChecks;
using Benzene.Examples.AwsMesh.Shared;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AwsMesh.Inventory;

/// <summary>
/// The inventory-api Cloud Service, hosted as an AWS Lambda. A pure event consumer: it publishes
/// nothing, but subscribes to <c>order:placed</c> (over SNS) and <c>shipping:dispatched</c> (over
/// EventBridge) — the same handlers reachable over every transport via the shared wiring, and still a
/// full Cloud Service Profile (spec/health) so the mesh discovers and renders it.
/// </summary>
public class Startup : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => MeshServiceWiring.ConfigureServices(services, "inventory", typeof(Startup).Assembly);

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks =
        {
            new InventoryDatabaseHealthCheck(),
        };

        MeshServiceWiring.Configure(app, "inventory",
            new[] { typeof(ReserveStockOnOrderPlacedHandler), typeof(DecrementStockOnShipmentDispatchedHandler) },
            healthChecks);
    }
}

/// <summary>AWS Lambda entry point hosting <see cref="Startup"/>, force-flushing OpenTelemetry per invocation.</summary>
public class Function : TracingLambdaHost<Startup>;
