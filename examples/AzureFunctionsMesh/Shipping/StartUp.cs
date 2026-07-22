using Benzene.Abstractions.Hosting;
using Benzene.Azure.Function.ServiceBus;
using Benzene.Core.MessageHandlers;
using Benzene.Diagnostics;
using Benzene.Examples.AzureFunctionsMesh.Shared;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AzureFunctionsMesh.Shipping;

/// <summary>
/// shipping-api, an Azure Function App Cloud Service. Consumes <c>shipment:book</c> off its <b>Service Bus
/// queue</b> (and over HTTP) — the terminal hop of the command chain.
/// </summary>
public class StartUp : BenzeneStartUp
{
    private static readonly Type[] Handlers = { typeof(BookShipmentHandler) };

    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => MeshServiceWiring.ConfigureServices(services, "shipping", Handlers);

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks = { new ServiceHealthCheck("shipping") };
        MeshServiceWiring.Configure(app, "shipping", Handlers, healthChecks, a => a
            .UseServiceBus(sb => sb
                .UseBenzeneEnrichment()
                .UseMessageHandlers(Handlers)));
    }
}
