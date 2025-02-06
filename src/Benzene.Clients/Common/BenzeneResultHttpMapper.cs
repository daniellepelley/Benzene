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
}
