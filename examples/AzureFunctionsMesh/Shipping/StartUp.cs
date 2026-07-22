using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Messages;
using Benzene.Azure.Function.ServiceBus;
using Benzene.Clients;
using Benzene.Clients.Azure.EventGrid;
using Benzene.Core.MessageHandlers;
using Benzene.Diagnostics;
using Benzene.Examples.AzureFunctionsMesh.Shared;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Benzene.ResponseEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AzureFunctionsMesh.Shipping;

/// <summary>
/// shipping-api, an Azure Function App Cloud Service. Consumes <c>shipment:book</c> off its <b>Service Bus
/// queue</b> (and over HTTP), and on booking publishes <c>shipment:dispatched</c> to Event Grid.
/// </summary>
public class StartUp : BenzeneStartUp
{
    private static readonly Type[] Handlers = { typeof(BookShipmentHandler) };

    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => MeshServiceWiring.ConfigureServices(services, "shipping", Handlers, x =>
        {
            x.AddResponseEventDeclarations((IMessageDefinition)new ResponseEventDefinition("shipment:dispatched", typeof(OutboundShipmentDispatched)));
            x.AddEventGridPublisher();
            x.AddOutboundRouting(routing => routing
                .Route("shipment:dispatched", pipeline => pipeline.UseEventGrid("shipping-api", eg => eg.UseEventGridClient())));
        });

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks = { new ServiceHealthCheck("shipping") };
        MeshServiceWiring.Configure(app, "shipping", Handlers, healthChecks, a => a
            .UseServiceBus(sb => sb
                .UseBenzeneEnrichment()
                .UseMessageHandlers(Handlers)));
    }
}
