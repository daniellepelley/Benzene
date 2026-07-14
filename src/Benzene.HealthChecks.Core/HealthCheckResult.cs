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
    public static Task<IHealthCheckResult> CreateInstance(Task<bool> success, string type)
    {
        return success.ContinueWith(x => CreateInstance(x.Result, type, new Dictionary<string, object>()));
    }

    /// <summary>Creates a result with diagnostic data, status <see cref="HealthCheckStatus.Ok"/> if <paramref name="success"/>, otherwise <see cref="HealthCheckStatus.Failed"/>.</summary>
    /// <param name="success">Whether the check succeeded.</param>
    /// <param name="type">The check's identifier.</param>
    /// <param name="data">Diagnostic details to include in the result.</param>
    public static IHealthCheckResult CreateInstance(bool success, string type, IDictionary<string, object> data)
    {
        return new HealthCheckResult(success ? HealthCheckStatus.Ok : HealthCheckStatus.Failed, type, data);
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

    /// <summary>Initializes a new instance of the <see cref="HealthCheckResult"/> class.</summary>
    /// <param name="status">One of <see cref="HealthCheckStatus.Ok"/>, <see cref="HealthCheckStatus.Warning"/>, or <see cref="HealthCheckStatus.Failed"/>.</param>
    /// <param name="type">The check's identifier.</param>
    /// <param name="data">Diagnostic details to include in the result.</param>
    public HealthCheckResult(string status, string type, IDictionary<string, object> data)
    {
        Status = status;
        Type = type;
        Data = data;
    }

    /// <inheritdoc />
    public string Status { get; }

    /// <inheritdoc />
    public string Type { get; }

    /// <inheritdoc />
    public IDictionary<string, object> Data { get; }
}
