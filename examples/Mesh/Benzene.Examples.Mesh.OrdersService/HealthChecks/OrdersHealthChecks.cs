using Benzene.HealthChecks.Core;

namespace Benzene.Examples.Mesh.OrdersService.HealthChecks;

/// <summary>
/// Reports a healthy PostgreSQL database check for orders-api, with a dependency on the
/// "orders-db" database - the mesh-ui detail view renders that as a dependency chip.
/// </summary>
public class OrdersDatabaseHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies =
    {
        new("Database", "orders-db"),
    };

    public string Type => "PostgresDatabase";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object> { ["latencyMs"] = 4, ["pool"] = "8/20" };

        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, Dependencies));
    }
}

/// <summary>
/// Reports a healthy Redis cache check for orders-api, with a dependency on the "orders-cache"
/// cache.
/// </summary>
public class OrdersCacheHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies =
    {
        new("Cache", "orders-cache"),
    };

    public string Type => "RedisCache";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object> { ["hitRate"] = "0.97" };

        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, Dependencies));
    }
}

/// <summary>
/// Reports a healthy SQS queue check for orders-api, with a dependency on the "order-events"
/// queue.
/// </summary>
public class OrdersQueueHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies =
    {
        new("Queue", "order-events"),
    };

    public string Type => "SqsQueue";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object> { ["approxDepth"] = 0 };

        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, Dependencies));
    }
}
