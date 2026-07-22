using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Messages;
using Benzene.Clients;
using Benzene.Clients.Azure.EventHub;
using Benzene.Clients.Azure.ServiceBus;
using Benzene.Examples.AzureFunctionsMesh.Shared;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Benzene.ResponseEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AzureFunctionsMesh.Orders;

/// <summary>
/// orders-api, an Azure Function App Cloud Service. Receives <c>order:create</c> over HTTP and, on
/// create, sends <c>payment:take</c> to payments-api over a <b>Service Bus queue</b> (a point-to-point
/// command). Interconnectivity only — its own inbound surface is HTTP (see Triggers.cs).
/// </summary>
public class StartUp : BenzeneStartUp
{
    private static readonly Type[] Handlers = { typeof(CreateOrderHandler) };

    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => MeshServiceWiring.ConfigureServices(services, "orders", Handlers, x =>
        {
            // Declare both sends in the spec's events → the mesh's structural edges.
            x.AddResponseEventDeclarations(
                (IMessageDefinition)new ResponseEventDefinition("payment:take", typeof(OutboundTakePayment)),
                new ResponseEventDefinition("order:placed", typeof(OutboundOrderPlaced)));

            // Lazy Service Bus sender (payments command queue) + Event Hub producer (order:placed fan-out).
            x.AddServiceBusSender(Environment.GetEnvironmentVariable("PAYMENTS_QUEUE") ?? "payments");
            x.AddEventHubProducer(Environment.GetEnvironmentVariable("ORDER_PLACED_HUB") ?? "order-placed");
            x.AddOutboundRouting(routing => routing
                .Route("payment:take", pipeline => pipeline.UseServiceBus(sb => sb.UseServiceBusClient()))
                .Route("order:placed", pipeline => pipeline.UseEventHub(eh => eh.UseEventHubClient())));
        });

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks = { new ServiceHealthCheck("orders") };
        MeshServiceWiring.Configure(app, "orders", Handlers, healthChecks);
    }
}
