using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.CloudService;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Examples.AwsMesh.Orders.HealthChecks;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Benzene.Schema.OpenApi;
using Benzene.Spec.Ui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AwsMesh.Orders;

/// <summary>
/// The orders-api Cloud Service, hosted as an AWS Lambda. Exposes the full Cloud Service profile over
/// HTTP (API Gateway) — <c>/benzene/invoke</c>, <c>/benzene/spec</c>, <c>/benzene/health</c>,
/// <c>/benzene/spec-ui</c> — and answers the mesh's direct Lambda-Invoke interrogation
/// (<c>spec</c>/<c>healthcheck</c> topics) on the BenzeneMessage pipeline.
/// </summary>
public class Startup : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.UsingBenzene(x => x.AddMessageHandlers(typeof(Startup).Assembly));
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        var region = System.Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-west-1";
        IHealthCheck[] healthChecks =
        {
            new OrdersDatabaseHealthCheck(),
            new OrdersQueueHealthCheck(),
        };

        app.UseAwsLambda(aws =>
        {
            // Public HTTP surface (API Gateway): the full Cloud Service profile + the Spec UI page.
            aws.UseApiGateway(http => http
                .UseSpecUi("/benzene/spec-ui", "/benzene/spec")
                .UseBenzeneCloudService("orders-api", cloud => cloud
                    .WithServiceVersion("1.0.0")
                    .WithInstanceId("orders-api")
                    .WithPlacement("aws", region)
                    .WithHealthChecks(healthChecks)));

            // Direct-invoke surface: how the mesh interrogates this Lambda with no HTTP surface needed
            // (LambdaMeshServiceSource sends the `spec` and `healthcheck` topics as an Invoke payload).
            aws.UseBenzeneMessage(bm => bm
                .UseHealthCheck("healthcheck", healthChecks)
                .UseSpec()
                .UseMessageHandlers());
        });
    }
}

/// <summary>AWS Lambda entry point hosting <see cref="Startup"/>.</summary>
public class Function : AwsLambdaHost<Startup>;
