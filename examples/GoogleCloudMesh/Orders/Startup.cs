using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Messages;
using Benzene.Clients;
using Benzene.Clients.GoogleCloud.PubSub;
using Benzene.Examples.GoogleCloudMesh.Shared;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Benzene.ResponseEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.GoogleCloudMesh.Orders;

/// <summary>orders-api: accepts orders over HTTP and publishes payment:take + order:placed to Pub/Sub.</summary>
public class Startup : BenzeneStartUp
{
    private static readonly Type[] Handlers = { typeof(CreateOrderHandler) };

    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => MeshServiceWiring.ConfigureServices(services, "orders", Handlers, x =>
        {
            // Declare produced events (→ the spec's events → the mesh topology).
            x.AddResponseEventDeclarations(
                (IMessageDefinition)new ResponseEventDefinition("payment:take", typeof(OutboundTakePayment)),
                new ResponseEventDefinition("order:placed", typeof(OutboundOrderPlaced)));
            x.AddPubSubPublisher();
            // Wire the runtime Pub/Sub routes: each Benzene topic → the target Pub/Sub topic (from env).
            x.AddOutboundRouting(routing => routing
                .Route("payment:take", pipeline => pipeline.UsePubSub(Environment.GetEnvironmentVariable("PAYMENT_TAKE_TOPIC") ?? "payment-take"))
                .Route("order:placed", pipeline => pipeline.UsePubSub(Environment.GetEnvironmentVariable("ORDER_PLACED_TOPIC") ?? "order-placed")));
        });

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks = { new ServiceHealthCheck("orders") };
        MeshServiceWiring.Configure(app, "orders", Handlers, healthChecks);
    }
}
