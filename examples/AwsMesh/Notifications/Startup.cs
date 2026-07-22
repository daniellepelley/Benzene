using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.Core;
using Benzene.Examples.AwsMesh.Notifications.Handlers;
using Benzene.Examples.AwsMesh.Notifications.HealthChecks;
using Benzene.Examples.AwsMesh.Shared;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AwsMesh.Notifications;

/// <summary>
/// The notifications-api Cloud Service, hosted as an AWS Lambda. A pure event consumer that subscribes
/// to <c>order:placed</c> (SNS fan-out, shared with inventory-api), plus <c>payment:captured</c> and
/// <c>shipping:dispatched</c> (EventBridge). Full Cloud Service Profile so the mesh discovers it.
/// </summary>
public class Startup : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => MeshServiceWiring.ConfigureServices(services, "notifications", typeof(Startup).Assembly);

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks =
        {
            new EmailProviderHealthCheck(),
        };

        MeshServiceWiring.Configure(app, "notifications",
            new[]
            {
                typeof(NotifyOnOrderPlacedHandler),
                typeof(NotifyOnPaymentCapturedHandler),
                typeof(NotifyOnShipmentDispatchedHandler),
            },
            healthChecks);
    }
}

/// <summary>AWS Lambda entry point hosting <see cref="Startup"/>, force-flushing OpenTelemetry per invocation.</summary>
public class Function : TracingLambdaHost<Startup>;
