using Benzene.HealthChecks.Core;

namespace Benzene.Examples.Mesh.PaymentsService.HealthChecks;

/// <summary>
/// Reports a healthy PostgreSQL database check for payments-api, with a dependency on the
/// "payments-db" database - the ok check alongside the failed gateway and the warning fraud-engine
/// gives the mesh-ui detail view all three per-check statuses at once.
/// </summary>
public class PaymentsDatabaseHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies =
    {
        new("Database", "payments-db"),
    };

    public string Type => "PostgresDatabase";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object> { ["latencyMs"] = 6 };

        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, Dependencies));
    }
}

/// <summary>
/// Reports a <see cref="HealthCheckStatus.Warning"/> (degraded but not failed) fraud-engine check for
/// payments-api, with a dependency on the "fraud-engine" HTTP service - the mesh-ui renders this as
/// an amber per-check status badge. A warning does not flip the aggregated response to unhealthy.
/// </summary>
public class FraudEngineHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies =
    {
        new("Http", "fraud-engine"),
    };

    public string Type => "FraudEngine";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object> { ["p99Ms"] = 850, ["note"] = "elevated latency" };

        return Task.FromResult(HealthCheckResult.CreateWarning(Type, data, Dependencies));
    }
}
