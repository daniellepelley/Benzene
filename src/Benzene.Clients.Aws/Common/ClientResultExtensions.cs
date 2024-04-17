using System;
using Benzene.Abstractions.Serialization;
using Benzene.Clients.Aws.Lambda;
using Benzene.Results;

namespace Benzene.Clients.Aws.Common
{
    public static class ClientResultExtensions
    {
        public static IClientResult<T> AsClientResult<T>(this BenzeneMessageClientResponse source, ISerializer serializer)
        {
            if (source.Message == null) return ReturnNullResult<T>(source);

            return typeof(T) == typeof(Guid) ? ReturnGuidResult<T>(source, serializer) : ReturnObjectResult<T>(source, serializer);
        }

        private static IClientResult<T> ReturnObjectResult<T>(BenzeneMessageClientResponse source, ISerializer serializer)
        {
            var clientStatusCode = ClientResultHttpMapper.MapClientResultStatus(source.StatusCode);
            switch (source.StatusCode)
            {
                case "200":
                case "201":
                case "202":
                case "204":
                    return source.Message == null 
                        ? ClientResult.Set<T>(clientStatusCode, true)
                        : ClientResult.Set(clientStatusCode, serializer.Deserialize<T>(source.Message));
                case "400":
                case "401":
                case "403":
                case "404":
                case "409":
                case "422":
                case "501":
                case "503":
                    return source.Message == null
                        ? ClientResult.Set<T>(clientStatusCode, false)
                        : ClientResult.Set<T>(clientStatusCode, serializer.Deserialize<ErrorPayload>(source.Message)?.Errors);
                default:
                    return ClientResult.UnexpectedError<T>("Status code {statusCode} not mapped", source.StatusCode);
            }
        }

        private static IClientResult<T> ReturnGuidResult<T>(BenzeneMessageClientResponse source, ISerializer serializer)
        {
            var clientStatusCode = ClientResultHttpMapper.MapClientResultStatus(source.StatusCode);
            switch (source.StatusCode)
            {
                case "200":
                case "201":
                case "202":
                case "204":
                    return (IClientResult<T>)ClientResult.Set(clientStatusCode, ParseGuid(source.Message, serializer));
                case "400":
                case "401":
                case "403":
                case "404":
                case "409":
                case "422":
                case "501":
                case "503":
                    return ClientResult.Set<T>(clientStatusCode, serializer.Deserialize<ErrorPayload>(source.Message)?.Errors);
                default:
                    return ClientResult.ServiceUnavailable<T>("Status code {statusCode} not mapped",
                        source.StatusCode);
            }
        }

        private static IClientResult<T> ReturnNullResult<T>(BenzeneMessageClientResponse source)
        {
            return ClientResultHttpMapper.Map<T>(source.StatusCode);
        }

        private static Guid ParseGuid(string message, ISerializer serializer)
        {
            var successfullyParsed = Guid.TryParse(serializer.Deserialize<string>(message), out var parsedGuid);

            if (!successfullyParsed)
            {
                parsedGuid = Guid.Empty;
            }

            return parsedGuid;
        }
    }
}
