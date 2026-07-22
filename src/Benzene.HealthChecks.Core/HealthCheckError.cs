using System;
using System.Collections.Generic;

namespace Benzene.HealthChecks.Core;

/// <summary>
/// Builds a classified failure result from a probe exception, applying the shared §3.4/§3.9 policy so
/// every client check reports failures the same way.
/// <list type="bullet">
/// <item>A permission/authorization error (HTTP <c>401</c>/<c>403</c>) is a
/// <see cref="HealthCheckStatus.Warning"/>, not a <see cref="HealthCheckStatus.Failed"/> — "I lack
/// permission to <em>probe</em> this" is not "the app is broken", so a least-privilege publisher stays
/// green rather than de-servicing the instance.</item>
/// <item>Any other failure (not-found, outage, timeout, bad connectivity) is
/// <see cref="HealthCheckStatus.Failed"/>.</item>
/// </list>
/// The exception <b>message</b> is never included (it may carry a connection string or other secret);
/// only the non-sensitive structured discriminators go into <c>Data</c> — the exception type, plus the
/// SDK's error code and HTTP status when the caller can supply them. "<c>403 / AuthorizationError</c>"
/// turns "something's wrong" into "it's IAM on this call", where the bare exception type is unactionable.
/// <para>
/// This lives in the cloud-agnostic core: the caller (an AWS/Azure client check) extracts the
/// <paramref name="errorCode"/>/<paramref name="statusCode"/> from its own SDK exception type and passes
/// them in, so the classification <em>policy</em> and <c>Data</c> shape are defined once without the core
/// taking any cloud-SDK dependency.
/// </para>
/// </summary>
public static class HealthCheckError
{
    /// <summary>Whether an HTTP status indicates a permission/authorization problem rather than an outage.</summary>
    public static bool IsPermissionStatus(int? statusCode) => statusCode is 401 or 403;

    /// <summary>
    /// Classifies <paramref name="exception"/> into a health-check result under the §3.4/§3.9 policy.
    /// </summary>
    /// <param name="type">The check's identifier (its <see cref="IHealthCheck.Type"/>).</param>
    /// <param name="exception">The exception the probe threw. Only its <em>type name</em> is reported, never its message.</param>
    /// <param name="dependencies">The dependencies the check verifies, preserved onto the result.</param>
    /// <param name="errorCode">The SDK's non-sensitive error code, if available (e.g. <c>"AuthorizationError"</c>).</param>
    /// <param name="statusCode">The HTTP status the SDK surfaced, if available. Drives the Warning-vs-Failed decision.</param>
    /// <param name="data">Optional check-specific diagnostic entries to include (e.g. the resource identifier). Never put secrets here.</param>
    /// <returns>A <see cref="HealthCheckStatus.Warning"/> result for a permission error, otherwise a <see cref="HealthCheckStatus.Failed"/> result.</returns>
    public static IHealthCheckResult Classify(string type, Exception exception, HealthCheckDependency[] dependencies,
        string? errorCode = null, int? statusCode = null, IDictionary<string, object>? data = null)
    {
        var payload = data ?? new Dictionary<string, object>();
        payload["Error"] = exception.GetType().Name;
        if (!string.IsNullOrEmpty(errorCode))
        {
            payload["ErrorCode"] = errorCode;
        }
        if (statusCode.HasValue)
        {
            payload["StatusCode"] = statusCode.Value;
        }

        return IsPermissionStatus(statusCode)
            ? HealthCheckResult.CreateWarning(type, payload, dependencies)
            : HealthCheckResult.CreateInstance(false, type, payload, dependencies);
    }
}
