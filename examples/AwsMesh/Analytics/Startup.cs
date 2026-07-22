using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.Core;
using Benzene.Examples.AwsMesh.Analytics.Handlers;
using Benzene.Examples.AwsMesh.Analytics.HealthChecks;
using Benzene.Examples.AwsMesh.Shared;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.AwsMesh.Analytics;

/// <summary>
/// The analytics-api Cloud Service, hosted as an AWS Lambda. A pure event consumer that subscribes to
/// <c>payment:captured</c> and <c>shipping:dispatched</c> over EventBridge (sharing those events with
/// notifications-api). Full Cloud Service Profile so the mesh discovers it.
/// </summary>
public class Startup : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => MeshServiceWiring.ConfigureServices(services, "analytics", typeof(Startup).Assembly);

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        IHealthCheck[] healthChecks =
        {
            new AnalyticsStoreHealthCheck(),
        };

        MeshServiceWiring.Configure(app, "analytics",
            new[] { typeof(RecordPaymentCapturedHandler), typeof(RecordShipmentDispatchedHandler) },
            healthChecks);
    }
}

/// <summary>AWS Lambda entry point hosting <see cref="Startup"/>, force-flushing OpenTelemetry per invocation.</summary>
public class Function : TracingLambdaHost<Startup>;
