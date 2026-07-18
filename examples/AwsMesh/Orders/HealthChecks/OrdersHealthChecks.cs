using Benzene.HealthChecks.Core;

namespace Benzene.Examples.AwsMesh.Orders.HealthChecks;

/// <summary>A healthy Postgres check for orders-api, declaring a dependency on the orders-db database.</summary>
public class OrdersDatabaseHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies = { new("Database", "orders-db") };

    public string Type => "PostgresDatabase";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object> { ["latencyMs"] = 4 };
        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, Dependencies));
    }
}

/// <summary>A healthy SQS check for orders-api, declaring a dependency on the order-events queue.</summary>
public class OrdersQueueHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies = { new("Queue", "order-events") };

    public string Type => "SqsQueue";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object> { ["approxDepth"] = 0 };
        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, Dependencies));
    }
}
