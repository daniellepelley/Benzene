using Benzene.HealthChecks.Core;

namespace Benzene.Examples.AwsMesh.Shipping.HealthChecks;

/// <summary>A healthy DynamoDB check for shipping-api, depending on the shipments table.</summary>
public class ShippingDatabaseHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies = { new("Database", "shipments-table") };

    public string Type => "DynamoDbTable";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object> { ["latencyMs"] = 3 };
        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, Dependencies));
    }
}

/// <summary>A healthy check for the downstream carrier API shipping-api integrates with.</summary>
public class CarrierApiHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies = { new("Http", "carrier-api") };

    public string Type => "CarrierApi";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object> { ["carriers"] = "DPD,RoyalMail" };
        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, Dependencies));
    }
}
