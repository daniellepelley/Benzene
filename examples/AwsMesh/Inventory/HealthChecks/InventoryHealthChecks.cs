using Benzene.HealthChecks.Core;

namespace Benzene.Examples.AwsMesh.Inventory.HealthChecks;

/// <summary>A healthy Postgres check for inventory-api, declaring a dependency on the inventory-db database.</summary>
public class InventoryDatabaseHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies = { new("Database", "inventory-db") };

    public string Type => "PostgresDatabase";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object> { ["latencyMs"] = 5 };
        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, Dependencies));
    }
}
