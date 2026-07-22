using Benzene.HealthChecks.Core;

namespace Benzene.Examples.AwsMesh.Notifications.HealthChecks;

/// <summary>A healthy check for notifications-api's outbound email provider, declaring a dependency on it.</summary>
public class EmailProviderHealthCheck : IHealthCheck
{
    private static readonly HealthCheckDependency[] Dependencies = { new("Http", "email-provider") };

    public string Type => "HttpDependency";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object> { ["latencyMs"] = 18 };
        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, Dependencies));
    }
}
