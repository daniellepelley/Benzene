using Benzene.HealthChecks.Core;

namespace Benzene.Examples.AwsMesh.Payments.HealthChecks;

/// <summary>A healthy Postgres check for payments-api, depending on the payments-db database.</summary>
public class PaymentsDatabaseHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies = { new("Database", "payments-db") };

    public string Type => "PostgresDatabase";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object> { ["latencyMs"] = 6 };
        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, Dependencies));
    }
}

/// <summary>A healthy check for the upstream payment gateway payments-api calls out to.</summary>
public class PaymentsGatewayHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies = { new("Http", "stripe-gateway") };

    public string Type => "PaymentGateway";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object> { ["provider"] = "stripe" };
        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, Dependencies));
    }
}
