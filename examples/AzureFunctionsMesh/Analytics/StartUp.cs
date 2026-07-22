using Benzene.Abstractions.Hosting;
using Benzene.Azure.Function.EventGrid;
using Benzene.Core.MessageHandlers;
using Benzene.Diagnostics;
using Benzene.Examples.AzureFunctionsMesh.Shared;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AzureFunctionsMesh.Analytics;

/// <summary>
/// analytics-api, an Azure Function App Cloud Service. A pure event consumer: records metrics on
/// <c>payment:captured</c> and <c>shipment:dispatched</c> over Event Grid (sharing those events with
/// notifications — one event → many subscriptions).
/// </summary>
public class StartUp : BenzeneStartUp
{
    private static readonly Type[] Handlers = { typeof(RecordPaymentCapturedHandler), typeof(RecordShipmentDispatchedHandler) };

    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => MeshServiceWiring.ConfigureServices(services, "analytics", Handlers);

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks = { new ServiceHealthCheck("analytics") };
        MeshServiceWiring.Configure(app, "analytics", Handlers, healthChecks, a => a
            .UseEventGrid(eg => eg.UseBenzeneEnrichment().UseMessageHandlers(Handlers)));
    }
}
