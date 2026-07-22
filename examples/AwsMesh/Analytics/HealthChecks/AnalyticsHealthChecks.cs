using Benzene.HealthChecks.Core;

namespace Benzene.Examples.AwsMesh.Analytics.HealthChecks;

/// <summary>A healthy check for analytics-api's metrics store, declaring a dependency on the warehouse.</summary>
public class AnalyticsStoreHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies = { new("Database", "analytics-warehouse") };

    public string Type => "TimeseriesStore";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object> { ["latencyMs"] = 9 };
        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, Dependencies));
    }
}
