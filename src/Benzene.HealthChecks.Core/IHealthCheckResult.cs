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

    /// <summary>
    /// The external dependencies this check verifies (e.g. a specific queue, database, or downstream
    /// service). A default interface member so existing implementers that don't override it stay
    /// source- and binary-compatible, defaulting to no reported dependencies.
    /// </summary>
    HealthCheckDependency[] Dependencies => Array.Empty<HealthCheckDependency>();

    /// <summary>
    /// How long the check took to run, filled in by the aggregating processor (an individual check
    /// need not set it). A default interface member so existing implementers stay source- and
    /// binary-compatible, defaulting to <see cref="TimeSpan.Zero"/>.
    /// </summary>
    TimeSpan Duration => TimeSpan.Zero;
}
