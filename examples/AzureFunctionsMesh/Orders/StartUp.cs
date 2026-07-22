using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Messages;
using Benzene.Clients;
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
            // Declare the send in the spec's events → the mesh's structural edge orders → payments.
            x.AddResponseEventDeclarations((IMessageDefinition)new ResponseEventDefinition("payment:take", typeof(OutboundTakePayment)));

            // Lazy Service Bus sender for the payments queue; the route publishes through it.
            x.AddServiceBusSender(Environment.GetEnvironmentVariable("PAYMENTS_QUEUE") ?? "payments");
            x.AddOutboundRouting(routing => routing
                .Route("payment:take", pipeline => pipeline.UseServiceBus(sb => sb.UseServiceBusClient())));
        });

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks = { new ServiceHealthCheck("orders") };
        MeshServiceWiring.Configure(app, "orders", Handlers, healthChecks);
    }
}
