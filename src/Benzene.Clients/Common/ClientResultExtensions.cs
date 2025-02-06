using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients.Aws.Lambda;
using Benzene.Results;

namespace Benzene.Clients.Common
{
    public static class BenzeneResultExtensions
    {
        public static IBenzeneResult<T> AsBenzeneResult<T>(this BenzeneMessageClientResponse source, ISerializer serializer)
        {
            if (source.Message == null) return ReturnNullResult<T>(source);

            return typeof(T) == typeof(Guid)
                ? ReturnGuidResult<T>(source, serializer)
                : ReturnObjectResult<T>(source, serializer);
        }

        private static IBenzeneResult<T> ReturnObjectResult<T>(BenzeneMessageClientResponse source, ISerializer serializer)
        {
            var clientStatusCode = BenzeneResultHttpMapper.MapBenzeneResultStatus(source.StatusCode);
            switch (source.StatusCode)
            {
                case "200":
                case "201":
                case "202":
                case "204":
                    return source.Message == null 
                        ? BenzeneResult.Set<T>(clientStatusCode, true)
                        : BenzeneResult.Set(clientStatusCode, serializer.Deserialize<T>(source.Message));
                case "400":
                case "401":
                case "403":
                case "404":
                case "409":
                case "422":
                case "501":
                case "503":
                    return source.Message == null
                        ? BenzeneResult.Set<T>(clientStatusCode, false)
                        : BenzeneResult.Set<T>(clientStatusCode, serializer.Deserialize<ErrorPayload>(source.Message).Detail);
                default:
                    return BenzeneResult.UnexpectedError<T>("Status code {statusCode} not mapped", source.StatusCode);
            }
        }

        private static IBenzeneResult<T> ReturnGuidResult<T>(BenzeneMessageClientResponse source, ISerializer serializer)
        {
            var clientStatusCode = BenzeneResultHttpMapper.MapBenzeneResultStatus(source.StatusCode);
            switch (source.StatusCode)
            {
                case "200":
                case "201":
                case "202":
                case "204":
                    return (IBenzeneResult<T>)BenzeneResult.Set(clientStatusCode, ParseGuid(source.Message, serializer));
                case "400":
                case "401":
                case "403":
                case "404":
                case "409":
                case "422":
                case "501":
                case "503":
                    var errorPayload =  serializer.Deserialize<ErrorPayload>(source.Message);

                    if (!string.IsNullOrEmpty(errorPayload?.Detail))
                    {
                        return BenzeneResult.Set<T>(clientStatusCode, errorPayload.Detail);
                    }
                    
                    return BenzeneResult.Set<T>(clientStatusCode);
                default:
                    return BenzeneResult.ServiceUnavailable<T>("Status code {statusCode} not mapped",
                        source.StatusCode);
            }
        }

        private static IBenzeneResult<T> ReturnNullResult<T>(BenzeneMessageClientResponse source)
        {
            return BenzeneResultHttpMapper.Map<T>(source.StatusCode);
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
