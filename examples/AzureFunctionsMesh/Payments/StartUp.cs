using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Messages;
using Benzene.Azure.Function.ServiceBus;
using Benzene.Clients;
using Benzene.Clients.Azure.EventGrid;
using Benzene.Clients.Azure.ServiceBus;
using Benzene.Core.MessageHandlers;
using Benzene.Diagnostics;
using Benzene.Examples.AzureFunctionsMesh.Shared;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Benzene.ResponseEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AzureFunctionsMesh.Payments;

/// <summary>
/// payments-api, an Azure Function App Cloud Service. Consumes <c>payment:take</c> off its <b>Service Bus
/// queue</b> (and over HTTP), and on payment sends <c>shipment:book</c> to shipping-api over Service Bus.
/// </summary>
public class StartUp : BenzeneStartUp
{
    private static readonly Type[] Handlers = { typeof(TakePaymentHandler) };

    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => MeshServiceWiring.ConfigureServices(services, "payments", Handlers, x =>
        {
            x.AddResponseEventDeclarations(
                (IMessageDefinition)new ResponseEventDefinition("shipment:book", typeof(OutboundBookShipment)),
                new ResponseEventDefinition("payment:captured", typeof(OutboundPaymentCaptured)));
            x.AddServiceBusSender(Environment.GetEnvironmentVariable("SHIPPING_QUEUE") ?? "shipping");
            x.AddEventGridPublisher();
            x.AddOutboundRouting(routing => routing
                .Route("shipment:book", pipeline => pipeline.UseServiceBus(sb => sb.UseServiceBusClient()))
                .Route("payment:captured", pipeline => pipeline.UseEventGrid("payments-api", eg => eg.UseEventGridClient())));
        });

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks = { new ServiceHealthCheck("payments") };
        MeshServiceWiring.Configure(app, "payments", Handlers, healthChecks, a => a
            // The same handler, now also reachable off the Service Bus queue trigger (routes by "topic").
            .UseServiceBus(sb => sb
                .UseBenzeneEnrichment()
                .UseMessageHandlers(Handlers)));
    }
}
