using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.Core;
using Benzene.Examples.AwsMesh.Orders.Handlers;
using Benzene.Examples.AwsMesh.Orders.HealthChecks;
using Benzene.Examples.AwsMesh.Orders.Model;
using Benzene.Examples.AwsMesh.Shared;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AwsMesh.Orders;

/// <summary>
/// The orders-api Cloud Service, hosted as an AWS Lambda. Via the shared wiring it exposes the full
/// Cloud Service Profile over HTTP, answers the mesh's direct-invoke interrogation, and routes its
/// domain handlers over SQS, SNS and EventBridge too — every pipeline logged with correlation ids and
/// every payload FluentValidation-checked.
/// </summary>
public class Startup : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => MeshServiceWiring.ConfigureServices(services, "orders", typeof(Startup).Assembly,
            // orders-api → payments-api: on create, send payments:capture to the payments SQS queue
            // (a point-to-point command — one consumer, must arrive).
            OutboundSend.Sqs("payments:capture", typeof(OutboundPaymentCapture), "PAYMENTS_QUEUE_URL"));

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks =
        {
            new OrdersDatabaseHealthCheck(),
            new OrdersQueueHealthCheck(),
        };

        MeshServiceWiring.Configure(app, "orders",
            new[] { typeof(GetOrdersMessageHandler), typeof(CreateOrderMessageHandler) },
            healthChecks);
    }
}

/// <summary>AWS Lambda entry point hosting <see cref="Startup"/>, force-flushing OpenTelemetry per invocation.</summary>
public class Function : TracingLambdaHost<Startup>;
