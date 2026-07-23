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

namespace Benzene.Examples.GoogleCloudMesh.Shipping;

/// <summary>shipping-api: consumes shipment:book (Pub/Sub or HTTP), publishes shipment:dispatched.</summary>
public class Startup : BenzeneStartUp
{
    private static readonly Type[] Handlers = { typeof(BookShipmentHandler) };

    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => MeshServiceWiring.ConfigureServices(services, "shipping", Handlers, x =>
        {
            x.AddResponseEventDeclarations(
                (IMessageDefinition)new ResponseEventDefinition("shipment:dispatched", typeof(OutboundShipmentDispatched)));
            x.AddPubSubPublisher();
            x.AddOutboundRouting(routing => routing
                .Route("shipment:dispatched", pipeline => pipeline.UsePubSub(Environment.GetEnvironmentVariable("SHIPMENT_DISPATCHED_TOPIC") ?? "shipment-dispatched")));
        });

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks = { new ServiceHealthCheck("shipping") };
        MeshServiceWiring.Configure(app, "shipping", Handlers, healthChecks);
    }
}
