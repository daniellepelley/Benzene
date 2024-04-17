using System;
using System.Net;
using Benzene.Results;

namespace Benzene.Clients.Aws.Common;

public static class ClientResultHttpMapper
{
    public static IClientResult<T> Map<T>(string statusCode)
    {
        switch (statusCode)
        {
            case "200":
            case "201":
            case "202":
            case "204":
                return ClientResult.Set<T>(MapClientResultStatus(statusCode), true);
            case "400":
            case "401":
            case "403":
            case "404":
            case "409":
            case "422":
            case "501":
            case "503":
                return ClientResult.Set<T>(MapClientResultStatus(statusCode), false);
            default:
                return ClientResult.UnexpectedError<T>("Status code {statusCode} not mapped", statusCode);
        }
    }
    
    public static string MapClientResultStatus(string statusCode)
    {
        switch (statusCode)
        {
            case "200":
            case "204":
                return ClientResultStatus.Ok;
            case "201":
                return ClientResultStatus.Created;
            case "202":
                return ClientResultStatus.Accepted;
            case "400":
                return ClientResultStatus.BadRequest;
            case "401":
                return ClientResultStatus.Unauthorized;
            case "403":
                return ClientResultStatus.Forbidden;
            case "404":
                return ClientResultStatus.NotFound;
            case "409":
                return ClientResultStatus.Conflict;
            case "422":
                return ClientResultStatus.ValidationError;
            case "501":
                return ClientResultStatus.NotImplemented;
            case "503":
                return ClientResultStatus.ServiceUnavailable;
            default:
                return ClientResultStatus.UnexpectedError;
        }
    }


    public static IClientResult<T> Map<T>(HttpStatusCode httpStatusCode)
    {
        return Map<T>(Convert.ToInt32(httpStatusCode).ToString());
    }
}
