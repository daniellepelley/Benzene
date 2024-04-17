namespace Benzene.HealthChecks.Core;

public interface IHealthCheck
{
    string Type { get; }
    Task<IHealthCheckResult> ExecuteAsync();
}

