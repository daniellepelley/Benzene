namespace Benzene.HealthChecks.Core;

/// <summary>Default <see cref="IHealthCheckResult"/> implementation, with factory methods covering the common status combinations.</summary>
public class HealthCheckResult : IHealthCheckResult
{
    /// <summary>The <see cref="IHealthCheckResult.Type"/> used by the parameterless <see cref="CreateInstance(bool)"/> overload, for callers that don't have a meaningful check identifier.</summary>
    public const string UnknownType = "Unknown";

    /// <summary>Creates a result of type <see cref="UnknownType"/>, healthy iff <paramref name="success"/>.</summary>
    /// <param name="success">Whether the check succeeded.</param>
    public static IHealthCheckResult CreateInstance(bool success)
    {
        return CreateInstance(success, UnknownType, new Dictionary<string, object>());
    }

    /// <summary>Creates a result with no diagnostic data, healthy iff <paramref name="success"/>.</summary>
    /// <param name="success">Whether the check succeeded.</param>
    /// <param name="type">The check's identifier.</param>
    public static IHealthCheckResult CreateInstance(bool success, string type)
    {
        return CreateInstance(success, type, new Dictionary<string, object>());
    }

    /// <summary>Awaits <paramref name="success"/> and creates the corresponding result with no diagnostic data.</summary>
    /// <param name="success">A task producing whether the check succeeded.</param>
    /// <param name="type">The check's identifier.</param>
    public static async Task<IHealthCheckResult> CreateInstance(Task<bool> success, string type)
    {
        // async/await, not ContinueWith: ContinueWith runs on TaskScheduler.Current (a footgun) and, on
        // a faulted source, x.Result surfaces an AggregateException that masks the real exception type.
        return CreateInstance(await success, type, new Dictionary<string, object>());
    }

    /// <summary>Creates a result with diagnostic data, status <see cref="HealthCheckStatus.Ok"/> if <paramref name="success"/>, otherwise <see cref="HealthCheckStatus.Failed"/>.</summary>
    /// <param name="success">Whether the check succeeded.</param>
    /// <param name="type">The check's identifier.</param>
    /// <param name="data">Diagnostic details to include in the result.</param>
    public static IHealthCheckResult CreateInstance(bool success, string type, IDictionary<string, object> data)
    {
        return new HealthCheckResult(success ? HealthCheckStatus.Ok : HealthCheckStatus.Failed, type, data);
    }

    /// <summary>Creates a result with diagnostic data and dependency metadata, status <see cref="HealthCheckStatus.Ok"/> if <paramref name="success"/>, otherwise <see cref="HealthCheckStatus.Failed"/>.</summary>
    /// <param name="success">Whether the check succeeded.</param>
    /// <param name="type">The check's identifier.</param>
    /// <param name="data">Diagnostic details to include in the result.</param>
    /// <param name="dependencies">The external dependencies this check verifies.</param>
    public static IHealthCheckResult CreateInstance(bool success, string type, IDictionary<string, object> data, HealthCheckDependency[] dependencies)
    {
        return new HealthCheckResult(success ? HealthCheckStatus.Ok : HealthCheckStatus.Failed, type, data, dependencies);
    }

    /// <summary>
    /// Creates a <b>persistent</b> <see cref="HealthCheckStatus.Failed"/> result with diagnostic data and
    /// dependency metadata - a deterministic fault (e.g. an authorization denial) that will not self-heal,
    /// so it is <b>not</b> softened by the non-critical downgrade (§3.4) and surfaces as unhealthy even for
    /// a dependency-category check. See <see cref="IHealthCheckResult.IsPersistent"/>.
    /// </summary>
    /// <param name="type">The check's identifier.</param>
    /// <param name="data">Diagnostic details to include in the result.</param>
    /// <param name="dependencies">The external dependencies this check verifies.</param>
    public static IHealthCheckResult CreatePersistentFailure(string type, IDictionary<string, object> data, HealthCheckDependency[] dependencies)
    {
        return new HealthCheckResult(HealthCheckStatus.Failed, type, data, dependencies, isPersistent: true);
    }

    /// <summary>Creates a <see cref="HealthCheckStatus.Warning"/> result with no diagnostic data - a degraded-but-not-failed outcome that does not flip an aggregated response's <c>IsHealthy</c> to <c>false</c>.</summary>
    /// <param name="type">The check's identifier.</param>
    public static IHealthCheckResult CreateWarning(string type)
    {
        return new HealthCheckResult(HealthCheckStatus.Warning, type, new Dictionary<string, object>());
    }

    /// <summary>Creates a <see cref="HealthCheckStatus.Warning"/> result with diagnostic data - a degraded-but-not-failed outcome that does not flip an aggregated response's <c>IsHealthy</c> to <c>false</c>.</summary>
    /// <param name="type">The check's identifier.</param>
    /// <param name="data">Diagnostic details to include in the result.</param>
    public static IHealthCheckResult CreateWarning(string type, IDictionary<string, object> data)
    {
        return new HealthCheckResult(HealthCheckStatus.Warning, type, data);
    }

    /// <summary>Creates a <see cref="HealthCheckStatus.Warning"/> result with diagnostic data and dependency metadata - a degraded-but-not-failed outcome that does not flip an aggregated response's <c>IsHealthy</c> to <c>false</c>.</summary>
    /// <param name="type">The check's identifier.</param>
    /// <param name="data">Diagnostic details to include in the result.</param>
    /// <param name="dependencies">The external dependencies this check verifies.</param>
    public static IHealthCheckResult CreateWarning(string type, IDictionary<string, object> data, HealthCheckDependency[] dependencies)
    {
        return new HealthCheckResult(HealthCheckStatus.Warning, type, data, dependencies);
    }

    /// <summary>Initializes a new instance of the <see cref="HealthCheckResult"/> class.</summary>
    /// <param name="status">One of <see cref="HealthCheckStatus.Ok"/>, <see cref="HealthCheckStatus.Warning"/>, or <see cref="HealthCheckStatus.Failed"/>.</param>
    /// <param name="type">The check's identifier.</param>
    /// <param name="data">Diagnostic details to include in the result.</param>
    /// <param name="dependencies">The external dependencies this check verifies. Defaults to none.</param>
    /// <param name="duration">How long the check took to run. Defaults to <see cref="TimeSpan.Zero"/>.</param>
    /// <param name="isPersistent">Whether a <see cref="HealthCheckStatus.Failed"/> result is a persistent, deterministic fault (see <see cref="IHealthCheckResult.IsPersistent"/>). Defaults to <c>false</c>.</param>
    /// <remarks>
    /// A single constructor with optional parameters, rather than overloads, so reflection-based
    /// deserializers (e.g. <c>Newtonsoft.Json</c>, which requires exactly one constructor to bind to
    /// when a type has no default constructor) keep working unambiguously.
    /// </remarks>
    public HealthCheckResult(string status, string type, IDictionary<string, object> data, HealthCheckDependency[]? dependencies = null, TimeSpan duration = default, bool isPersistent = false)
    {
        Status = status;
        Type = type;
        Data = data;
        Dependencies = dependencies ?? Array.Empty<HealthCheckDependency>();
        Duration = duration;
        IsPersistent = isPersistent;
    }

    /// <inheritdoc />
    public string Status { get; }

    /// <inheritdoc />
    public string Type { get; }

    /// <inheritdoc />
    public IDictionary<string, object> Data { get; }

    /// <inheritdoc />
    public HealthCheckDependency[] Dependencies { get; }

    /// <inheritdoc />
    public TimeSpan Duration { get; }

    /// <inheritdoc />
    public bool IsPersistent { get; }
}
