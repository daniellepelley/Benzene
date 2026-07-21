using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.Core;
using Benzene.Examples.AwsMesh.Shared;
using Benzene.Examples.AwsMesh.Shipping.Handlers;
using Benzene.Examples.AwsMesh.Shipping.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AwsMesh.Shipping;

/// <summary>
/// The shipping-api Cloud Service, hosted as an AWS Lambda. Via the shared wiring it exposes the full
/// Cloud Service Profile over HTTP, answers the mesh's direct-invoke interrogation, and routes its
/// domain handlers over SQS, SNS and EventBridge too — every pipeline logged and every payload validated.
/// </summary>
public class Startup : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => MeshServiceWiring.ConfigureServices(services, "shipping", typeof(Startup).Assembly);

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks =
        {
            new ShippingDatabaseHealthCheck(),
            new CarrierApiHealthCheck(),
        };

        MeshServiceWiring.Configure(app, "shipping",
            new[] { typeof(GetShipmentsMessageHandler), typeof(BookShipmentMessageHandler) },
            healthChecks);
    }
}

/// <summary>AWS Lambda entry point hosting <see cref="Startup"/>.</summary>
public class Function : TracingLambdaHost<Startup>;
