using Benzene.HealthChecks.Core;

namespace Benzene.Examples.Mesh.PaymentsService.HealthChecks;

/// <summary>
/// Reports <b>failed by default</b> (the payments gateway is down), so payments-api shows as
/// unhealthy out of the box - set <c>DEMO_PAYMENTS_HEALTHY=true</c> at startup and restart to see it
/// report ok instead. Always reports a dependency on a "stripe-gateway" HTTP service, so the mesh-ui
/// detail view has a real dependency chip to show regardless of status (see
/// <c>Benzene.HealthChecks.Core.HealthCheckDependency</c>).
/// </summary>
public class PaymentsGatewayHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies =
    {
        new("Http", "stripe-gateway"),
    };

    public string Type => "PaymentsGateway";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var isHealthy = Environment.GetEnvironmentVariable("DEMO_PAYMENTS_HEALTHY") == "true";

        var data = isHealthy
            ? new Dictionary<string, object>()
            : new Dictionary<string, object> { ["reason"] = "gateway timeout" };

        return Task.FromResult(HealthCheckResult.CreateInstance(isHealthy, Type, data, Dependencies));
    }
}
