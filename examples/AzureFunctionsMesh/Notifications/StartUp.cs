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

namespace Benzene.Examples.AzureFunctionsMesh.Notifications;

/// <summary>
/// notifications-api, an Azure Function App Cloud Service. A pure event consumer: notifies the customer on
/// <c>order:placed</c> (Event Hub, fanned out alongside inventory), <c>payment:captured</c> and
/// <c>shipment:dispatched</c> (Event Grid).
/// </summary>
public class StartUp : BenzeneStartUp
{
    private static readonly Type[] Handlers =
    {
        typeof(NotifyOnOrderPlacedHandler),
        typeof(NotifyOnPaymentCapturedHandler),
        typeof(NotifyOnShipmentDispatchedHandler),
    };

    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => MeshServiceWiring.ConfigureServices(services, "notifications", Handlers);

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks = { new ServiceHealthCheck("notifications") };
        MeshServiceWiring.Configure(app, "notifications", Handlers, healthChecks, a => a
            .UseEventHub(eh => eh.UseBenzeneEnrichment().UseMessageHandlers(Handlers))
            .UseEventGrid(eg => eg.UseBenzeneEnrichment().UseMessageHandlers(Handlers)));
    }
}
