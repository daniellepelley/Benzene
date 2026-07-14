using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>
/// A trivial <see cref="IHealthCheck"/> that always reports success with no diagnostic data. Useful as a
/// smoke test that the health check pipeline itself is wired up and reachable, or as a placeholder while
/// building out real checks.
/// </summary>
public class SimpleHealthCheck : IHealthCheck
{
    /// <inheritdoc />
    public string Type => "Simple";

    /// <summary>Always returns a successful result.</summary>
    public Task<IHealthCheckResult> ExecuteAsync()
    {
        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type));
    }
}
