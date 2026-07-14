using System.Net;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Clients.Common;

public static class BenzeneResultHttpMapper
{
    public static IBenzeneResult<T> Map<T>(string statusCode)
    {
        switch (statusCode)
        {
            case "200":
            case "201":
            case "202":
            case "204":
                return BenzeneResult.Set<T>(MapBenzeneResultStatus(statusCode), true);
            case "400":
            case "401":
            case "403":
            case "404":
            case "409":
            case "422":
            case "501":
            case "503":
                return BenzeneResult.Set<T>(MapBenzeneResultStatus(statusCode), false);
            default:
                return BenzeneResult.UnexpectedError<T>("Status code {statusCode} not mapped", statusCode);
        }
    }
    
    public static string MapBenzeneResultStatus(string statusCode)
    {
        switch (statusCode)
        {
            case "200":
            case "204":
                return BenzeneResultStatus.Ok;
            case "201":
                return BenzeneResultStatus.Created;
            case "202":
                return BenzeneResultStatus.Accepted;
            case "400":
                return BenzeneResultStatus.BadRequest;
            case "401":
                return BenzeneResultStatus.Unauthorized;
            case "403":
                return BenzeneResultStatus.Forbidden;
            case "404":
                return BenzeneResultStatus.NotFound;
            case "409":
                return BenzeneResultStatus.Conflict;
            case "422":
                return BenzeneResultStatus.ValidationError;
            case "501":
                return BenzeneResultStatus.NotImplemented;
            case "503":
                return BenzeneResultStatus.ServiceUnavailable;
            default:
                return BenzeneResultStatus.UnexpectedError;
        }
    }


    public static IBenzeneResult<T> Map<T>(HttpStatusCode httpStatusCode)
    {
        return Map<T>(Convert.ToInt32(httpStatusCode).ToString());
    }

    private static readonly string[] SuccessStatuses =
    {
        BenzeneResultStatus.Ok,
        BenzeneResultStatus.Created,
        BenzeneResultStatus.Accepted,
        BenzeneResultStatus.Updated,
        BenzeneResultStatus.Deleted,
        BenzeneResultStatus.Ignored
    };

    private static readonly string[] FailureStatuses =
    {
        BenzeneResultStatus.BadRequest,
        BenzeneResultStatus.ValidationError,
        BenzeneResultStatus.Unauthorized,
        BenzeneResultStatus.Forbidden,
        BenzeneResultStatus.NotFound,
        BenzeneResultStatus.Conflict,
        BenzeneResultStatus.NotImplemented,
        BenzeneResultStatus.ServiceUnavailable,
        BenzeneResultStatus.UnexpectedError
    };

    /// <summary>
    /// Normalizes a response's status code to a Benzene result status. A raw Benzene status (the
    /// standard envelope contract - what <c>BenzeneMessageResponse.StatusCode</c> carries) passes
    /// through verbatim, preserving distinctions like <c>Updated</c> vs <c>Ok</c>; a numeric HTTP
    /// status code (older or HTTP-shaped services) is mapped via <see cref="MapBenzeneResultStatus"/>.
    /// Returns <c>null</c> for anything unrecognized.
    /// </summary>
    public static string? NormalizeStatus(string? statusCode)
    {
        if (string.IsNullOrEmpty(statusCode))
        {
            return null;
        }

        if (SuccessStatuses.Contains(statusCode) || FailureStatuses.Contains(statusCode))
        {
            return statusCode;
        }

        switch (statusCode)
        {
            case "200":
            case "201":
            case "202":
            case "204":
            case "400":
            case "401":
            case "403":
            case "404":
            case "409":
            case "422":
            case "500":
            case "501":
            case "503":
                return MapBenzeneResultStatus(statusCode);
            default:
                return null;
        }
    }

    /// <summary>Whether a (normalized) Benzene result status is a success status.</summary>
    public static bool IsSuccessStatus(string status)
    {
        return SuccessStatuses.Contains(status);
    }
}
