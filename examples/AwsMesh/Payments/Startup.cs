using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.Core;
using Benzene.Examples.AwsMesh.Payments.Handlers;
using Benzene.Examples.AwsMesh.Payments.HealthChecks;
using Benzene.Examples.AwsMesh.Payments.Model;
using Benzene.Examples.AwsMesh.Shared;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AwsMesh.Payments;

/// <summary>
/// The payments-api Cloud Service, hosted as an AWS Lambda. Via the shared wiring it exposes the full
/// Cloud Service Profile over HTTP, answers the mesh's direct-invoke interrogation, and routes its
/// domain handlers over SQS, SNS and EventBridge too — every pipeline logged and every payload validated.
/// </summary>
public class Startup : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => MeshServiceWiring.ConfigureServices(services, "payments", typeof(Startup).Assembly,
            // payments-api → shipping-api: on capture, send shipping:book to the shipping SQS queue
            // (a point-to-point command — one consumer, must arrive).
            OutboundSend.Sqs("shipping:book", typeof(OutboundShipmentBook), "SHIPPING_QUEUE_URL"),
            // payments-api → notifications-api + analytics-api: publish payment:captured to EventBridge,
            // routed to interested consumers by rule (an integration event).
            OutboundSend.EventBridge("payment:captured", typeof(OutboundPaymentCaptured), "EVENT_BUS_NAME"));

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks =
        {
            new PaymentsDatabaseHealthCheck(),
            new PaymentsGatewayHealthCheck(),
        };

        MeshServiceWiring.Configure(app, "payments",
            new[] { typeof(GetPaymentsMessageHandler), typeof(CapturePaymentMessageHandler) },
            healthChecks);
    }
}

/// <summary>AWS Lambda entry point hosting <see cref="Startup"/>.</summary>
public class Function : TracingLambdaHost<Startup>;
