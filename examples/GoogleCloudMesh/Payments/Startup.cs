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

namespace Benzene.Examples.GoogleCloudMesh.Payments;

/// <summary>payments-api: consumes payment:take (Pub/Sub or HTTP), publishes shipment:book + payment:captured.</summary>
public class Startup : BenzeneStartUp
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
            x.AddPubSubPublisher();
            x.AddOutboundRouting(routing => routing
                .Route("shipment:book", pipeline => pipeline.UsePubSub(Environment.GetEnvironmentVariable("SHIPMENT_BOOK_TOPIC") ?? "shipment-book"))
                .Route("payment:captured", pipeline => pipeline.UsePubSub(Environment.GetEnvironmentVariable("PAYMENT_CAPTURED_TOPIC") ?? "payment-captured")));
        });

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks = { new ServiceHealthCheck("payments") };
        MeshServiceWiring.Configure(app, "payments", Handlers, healthChecks);
    }
}
