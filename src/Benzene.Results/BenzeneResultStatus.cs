namespace Benzene.Results;

/// <summary>
/// The framework-defined result status vocabulary (see
/// <c>docs/specification/wire-contracts.md</c> §3 — the strings are the case-sensitive wire
/// values, **lowercase-kebab-case** e.g. <c>not-found</c>/<c>validation-error</c>), plus the
/// classification of each status. Applications may use additional status strings; every transport
/// mapping routes unknown statuses to its generic-error row, and the classifiers below treat them
/// as neither success nor failure.
/// </summary>
public static class BenzeneResultStatus
{
    public const string Accepted = "accepted";
    public const string Ok = "ok";
    public const string Created = "created";
    public const string Updated = "updated";
    public const string Deleted = "deleted";
    public const string Ignored = "ignored";
    public const string NotFound = "not-found";
    public const string BadRequest = "bad-request";
    public const string ValidationError = "validation-error";
    public const string ServiceUnavailable = "service-unavailable";
    public const string NotImplemented = "not-implemented";
    public const string UnexpectedError = "unexpected-error";
    public const string Conflict = "conflict";
    public const string Forbidden = "forbidden";
    public const string Unauthorized = "unauthorized";
    public const string TooManyRequests = "too-many-requests";
    public const string Timeout = "timeout";

    private static readonly HashSet<string> SuccessStatuses = new()
    {
        Ok,
        Created,
        Accepted,
        Updated,
        Deleted,
        Ignored
    };

    private static readonly HashSet<string> FailureStatuses = new()
    {
        BadRequest,
        ValidationError,
        Unauthorized,
        Forbidden,
        NotFound,
        Conflict,
        TooManyRequests,
        Timeout,
        NotImplemented,
        ServiceUnavailable,
        UnexpectedError
    };

    private static readonly HashSet<string> TransientStatuses = new()
    {
        ServiceUnavailable,
        TooManyRequests,
        Timeout
    };

    /// <summary>
    /// True when the status is one of the framework-defined success statuses
    /// (<see cref="Ok"/>, <see cref="Created"/>, <see cref="Accepted"/>, <see cref="Updated"/>,
    /// <see cref="Deleted"/>, <see cref="Ignored"/>). False for failure, unknown, and null statuses.
    /// </summary>
    public static bool IsSuccess(string? status)
    {
        return status != null && SuccessStatuses.Contains(status);
    }

    /// <summary>
    /// True when the status is one of the framework-defined failure statuses. False for success,
    /// unknown, and null statuses — an application-defined status is not assumed to be a failure.
    /// </summary>
    public static bool IsFailure(string? status)
    {
        return status != null && FailureStatuses.Contains(status);
    }

    /// <summary>
    /// True when the status is part of the framework-defined vocabulary (success or failure).
    /// </summary>
    public static bool IsKnown(string? status)
    {
        return IsSuccess(status) || IsFailure(status);
    }

    /// <summary>
    /// True when the status describes a transient condition — one where a later retry may
    /// succeed: <see cref="ServiceUnavailable"/>, <see cref="TooManyRequests"/>, and
    /// <see cref="Timeout"/>. Note that transient does not always mean retry-*safe*: a
    /// <see cref="Timeout"/> leaves it unknown whether the operation was applied, so blind
    /// retries are only appropriate for idempotent operations (see
    /// <c>RetryBenzeneMessageClient</c>, which excludes <see cref="Timeout"/> by default).
    /// </summary>
    public static bool IsTransient(string? status)
    {
        return status != null && TransientStatuses.Contains(status);
    }
}
