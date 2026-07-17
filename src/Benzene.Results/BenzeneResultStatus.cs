namespace Benzene.Results;

/// <summary>
/// The framework-defined result status vocabulary (see
/// <c>docs/specification/wire-contracts.md</c> §3 — the strings are the case-sensitive wire
/// values), plus the classification of each status. Applications may use additional status
/// strings; every transport mapping routes unknown statuses to its generic-error row, and the
/// classifiers below treat them as neither success nor failure.
/// </summary>
public static class BenzeneResultStatus
{
    public const string Accepted = "Accepted";
    public const string Ok = "Ok";
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";
    public const string Ignored = "Ignored";
    public const string NotFound = "NotFound";
    public const string BadRequest = "BadRequest";
    public const string ValidationError = "ValidationError";
    public const string ServiceUnavailable = "ServiceUnavailable";
    public const string NotImplemented = "NotImplemented";
    public const string UnexpectedError = "UnexpectedError";
    public const string Conflict = "Conflict";
    public const string Forbidden = "Forbidden";
    public const string Unauthorized = "Unauthorized";
    public const string TooManyRequests = "TooManyRequests";
    public const string Timeout = "Timeout";

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
