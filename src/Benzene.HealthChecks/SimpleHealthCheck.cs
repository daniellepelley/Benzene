using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

public class SimpleHealthCheck : IHealthCheck
{
    public string Type => "Simple";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type));
    }
}
