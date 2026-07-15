using Benzene.HealthChecks.Core;

namespace Benzene.Examples.Mesh.ShippingService.HealthChecks;

/// <summary>
/// Reports a healthy carrier-API check for shipping-api, with a dependency on the "fedex-api"
/// HTTP service. shipping-api is deliberately not started by <c>run.sh</c>, so the dashboard shows
/// it as unreachable - but if you start it manually these checks render richly.
/// </summary>
public class ShippingCarrierApiHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies =
    {
        new("Http", "fedex-api"),
    };

    public string Type => "CarrierApi";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object>();

        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, Dependencies));
    }
}

/// <summary>
/// Reports a healthy SQS queue check for shipping-api, with a dependency on the "shipment-events"
/// queue.
/// </summary>
public class ShippingQueueHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies =
    {
        new("Queue", "shipment-events"),
    };

    public string Type => "SqsQueue";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object>();

        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, Dependencies));
    }
}
