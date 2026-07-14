namespace Benzene.HealthChecks.Core;

/// <summary>The outcome of running one <see cref="IHealthCheck"/>.</summary>
public interface IHealthCheckResult
{
    /// <summary>One of <see cref="HealthCheckStatus.Ok"/>, <see cref="HealthCheckStatus.Warning"/>, or <see cref="HealthCheckStatus.Failed"/>.</summary>
    string Status { get; }

    /// <summary>The identifier of the check that produced this result (matches <see cref="IHealthCheck.Type"/>).</summary>
    string Type { get; }

    /// <summary>Arbitrary diagnostic details specific to the check (e.g. the URL pinged, the exception message), surfaced in the aggregated response.</summary>
    IDictionary<string, object> Data { get; }
}
