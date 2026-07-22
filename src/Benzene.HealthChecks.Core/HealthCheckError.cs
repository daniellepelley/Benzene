using System;
using System.Collections.Generic;

namespace Benzene.HealthChecks.Core;

/// <summary>
/// Builds a classified failure result from a probe exception, applying the shared §3.4/§3.9 policy so
/// every client check reports failures the same way.
/// <list type="bullet">
/// <item>A permission/authorization error is a <b>persistent</b> <see cref="HealthCheckStatus.Failed"/>
/// (<see cref="IHealthCheckResult.IsPersistent"/>). It is a deterministic misconfiguration - a missing
/// permission or bad credentials - that will not self-heal, so it must <em>not</em> be softened by the
/// non-critical downgrade (§3.4): it surfaces as unhealthy on the deep <c>healthcheck</c> layer even for
/// an auto-wired dependency check. Detected by <em>meaning</em> (HTTP 401/403 or a known authorization
/// error code), so the same denial classifies identically whether the SDK returns 403 or - like AWS
/// EventBridge's <c>AccessDeniedException</c> - HTTP 400. This is safe because the deep <c>healthcheck</c>
/// layer (the only one that harvests dependency checks, and the status the Mesh UI renders) is
/// <b>advisory</b>: <c>liveness</c>/<c>readiness</c> exclude dependency checks, so a red here can never
/// de-service a pod or de-register a load balancer target - it tells a human the estate is not wired up as
/// expected. So surfacing an authorization denial as red is a true, useful signal, not a false alarm.
/// (Reversal of the earlier §3.9 rule, which made a permission error a Warning so a least-privilege publisher
/// stayed green. <c>healthCheck: false</c> on the wiring stops probing a dependency you don't want monitored
/// at all - it is not a workaround for the advisory red.)</item>
/// <item>Any other failure (not-found, outage, timeout, bad connectivity, throttling) is a transient
/// <see cref="HealthCheckStatus.Failed"/> that the non-critical downgrade still softens to a Warning.</item>
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
    // Well-known SDK error codes that mean "not authorized" regardless of the HTTP status number the SDK
    // happens to surface. Kept small and well-known; the 401/403 status check is always the fallback, so
    // an unrecognized code still classifies correctly on status. Case-insensitive.
    private static readonly HashSet<string> AuthorizationErrorCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "AccessDenied",             // AWS S3 and others
        "AccessDeniedException",    // AWS EventBridge/Lambda/StepFunctions (surfaced as HTTP 400)
        "AuthorizationError",       // AWS SNS
        "AuthorizationFailure",     // Azure
        "AuthenticationFailed",     // Azure Storage
        "Forbidden",
        "Unauthorized",
    };

    /// <summary>Whether an HTTP status indicates a permission/authorization problem rather than an outage.</summary>
    public static bool IsPermissionStatus(int? statusCode) => statusCode is 401 or 403;

    /// <summary>
    /// Whether a failure is an authorization/permission denial - a <em>persistent</em>, deterministic fault
    /// (missing IAM permission, bad credentials) that will not self-heal. Detected by the error's
    /// <em>meaning</em>: HTTP 401/403 <b>or</b> a known authorization <paramref name="errorCode"/> - because
    /// some SDKs (e.g. AWS EventBridge) surface an <c>AccessDeniedException</c> as HTTP 400, so keying on the
    /// status number alone would misclassify it.
    /// </summary>
    public static bool IsAuthorizationFailure(int? statusCode, string? errorCode = null)
        => IsPermissionStatus(statusCode)
           || (!string.IsNullOrEmpty(errorCode) && AuthorizationErrorCodes.Contains(errorCode));

    /// <summary>
    /// Classifies <paramref name="exception"/> into a health-check result under the §3.4/§3.9 policy.
    /// </summary>
    /// <param name="type">The check's identifier (its <see cref="IHealthCheck.Type"/>).</param>
    /// <param name="exception">The exception the probe threw. Only its <em>type name</em> is reported, never its message.</param>
    /// <param name="dependencies">The dependencies the check verifies, preserved onto the result.</param>
    /// <param name="errorCode">The SDK's non-sensitive error code, if available (e.g. <c>"AuthorizationError"</c>).</param>
    /// <param name="statusCode">The HTTP status the SDK surfaced, if available. Feeds the authorization detection alongside <paramref name="errorCode"/>.</param>
    /// <param name="data">Optional check-specific diagnostic entries to include (e.g. the resource identifier). Never put secrets here.</param>
    /// <returns>A <b>persistent</b> <see cref="HealthCheckStatus.Failed"/> for an authorization denial, otherwise a transient <see cref="HealthCheckStatus.Failed"/>.</returns>
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

        // An authorization denial is a persistent fault that escapes the non-critical downgrade; any other
        // failure is a transient Failed the downgrade may still soften to a Warning for a dependency check.
        return IsAuthorizationFailure(statusCode, errorCode)
            ? HealthCheckResult.CreatePersistentFailure(type, payload, dependencies)
            : HealthCheckResult.CreateInstance(false, type, payload, dependencies);
    }
}
