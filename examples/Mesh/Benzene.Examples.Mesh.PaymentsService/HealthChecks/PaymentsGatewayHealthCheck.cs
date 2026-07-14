using Benzene.HealthChecks.Core;

namespace Benzene.Examples.Mesh.PaymentsService.HealthChecks;

/// <summary>
/// Reports healthy unless <c>DEMO_UNHEALTHY=true</c> is set at startup - flip that env var and
/// restart to see the mesh dashboard's unhealthy badge. Always reports a dependency on a
/// "payments-gateway" HTTP service, so the mesh-ui detail view has a real dependency chip to show
/// regardless of status (see <c>Benzene.HealthChecks.Core.HealthCheckDependency</c>).
/// </summary>
public class PaymentsGatewayHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies =
    {
        new("Http", "payments-gateway"),
    };

    public string Type => "PaymentsGateway";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var isHealthy = Environment.GetEnvironmentVariable("DEMO_UNHEALTHY") != "true";

        var data = isHealthy
            ? new Dictionary<string, object>()
            : new Dictionary<string, object> { ["reason"] = "payments-gateway unreachable" };

        return Task.FromResult(HealthCheckResult.CreateInstance(isHealthy, Type, data, Dependencies));
    }
}
