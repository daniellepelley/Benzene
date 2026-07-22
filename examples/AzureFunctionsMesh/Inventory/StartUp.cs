using Benzene.Abstractions.Hosting;
using Benzene.Azure.Function.EventGrid;
using Benzene.Azure.Function.EventHub.Function;
using Benzene.Core.MessageHandlers;
using Benzene.Diagnostics;
using Benzene.Examples.AzureFunctionsMesh.Shared;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AzureFunctionsMesh.Inventory;

/// <summary>
/// inventory-api, an Azure Function App Cloud Service. A pure event consumer: reserves stock on
/// <c>order:placed</c> (Event Hub) and decrements on <c>shipment:dispatched</c> (Event Grid). Full Cloud
/// Service Profile over HTTP so the mesh discovers and renders it.
/// </summary>
public class StartUp : BenzeneStartUp
{
    private static readonly Type[] Handlers = { typeof(ReserveStockHandler), typeof(DecrementStockHandler) };

    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => MeshServiceWiring.ConfigureServices(services, "inventory", Handlers);

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks = { new ServiceHealthCheck("inventory") };
        MeshServiceWiring.Configure(app, "inventory", Handlers, healthChecks, a => a
            // order:placed off Event Hub (property-based routing), shipment:dispatched off Event Grid.
            .UseEventHub(eh => eh.UseBenzeneEnrichment().UseMessageHandlers(Handlers))
            .UseEventGrid(eg => eg.UseBenzeneEnrichment().UseMessageHandlers(Handlers)));
    }
}
