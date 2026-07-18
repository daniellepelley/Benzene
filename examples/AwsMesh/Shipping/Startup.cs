using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.CloudService;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Examples.AwsMesh.Shipping.HealthChecks;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Benzene.Schema.OpenApi;
using Benzene.Spec.Ui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AwsMesh.Shipping;

/// <summary>The shipping-api Cloud Service, hosted as an AWS Lambda (see the Orders service for the shape).</summary>
public class Startup : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // AddBenzene registers the baseline every Benzene app needs (IDefaultStatuses, serializer,
        // version selection, core middleware); UseBenzeneCloudService/UseApiGateway don't add it.
        services.UsingBenzene(x => x.AddBenzene().AddMessageHandlers(typeof(Startup).Assembly));
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        var region = System.Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-west-1";
        IHealthCheck[] healthChecks =
        {
            new ShippingDatabaseHealthCheck(),
            new CarrierApiHealthCheck(),
        };

        app.UseAwsLambda(aws =>
        {
            aws.UseApiGateway(http => http
                .UseSpecUi("/benzene/spec-ui", "/benzene/spec")
                .UseBenzeneCloudService("shipping-api", cloud => cloud
                    .WithServiceVersion("1.0.0")
                    .WithInstanceId("shipping-api")
                    .WithPlacement("aws", region)
                    .WithHealthChecks(healthChecks)));

            aws.UseBenzeneMessage(bm => bm
                .UseHealthCheck("healthcheck", healthChecks)
                .UseSpec()
                .UseMessageHandlers());
        });
    }
}

/// <summary>AWS Lambda entry point hosting <see cref="Startup"/>.</summary>
public class Function : AwsLambdaHost<Startup>;
