namespace Benzene.HealthChecks.Core;

public interface IHealthCheckResult
{
    string Status { get; }
    string Type { get; }
    IDictionary<string, object> Data { get; }
}
