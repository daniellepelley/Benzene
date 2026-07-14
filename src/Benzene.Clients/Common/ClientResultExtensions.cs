using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Results;

namespace Benzene.Clients.Common
{
    public static class BenzeneResultExtensions
    {
        /// <summary>
        /// Maps a <see cref="BenzeneMessageClientResponse"/> to an <see cref="IBenzeneResult{T}"/>.
        /// The response's status code may be a raw Benzene result status (the standard envelope
        /// contract, preserved verbatim) or a numeric HTTP status code (older or HTTP-shaped
        /// services, mapped to its Benzene equivalent). Failure bodies are read as the standard
        /// error payload (<c>{ "status": ..., "detail": ... }</c>).
        /// </summary>
        public static IBenzeneResult<T> AsBenzeneResult<T>(this BenzeneMessageClientResponse source, ISerializer serializer)
        {
            var status = BenzeneResultHttpMapper.NormalizeStatus(source.StatusCode);
            if (status == null)
            {
                return BenzeneResult.UnexpectedError<T>("Status code {statusCode} not mapped", source.StatusCode);
            }

            if (source.Body == null)
            {
                return BenzeneResult.Set<T>(status, BenzeneResultHttpMapper.IsSuccessStatus(status));
            }

            return typeof(T) == typeof(Guid)
                ? ReturnGuidResult<T>(status, source.Body, serializer)
                : ReturnObjectResult<T>(status, source.Body, serializer);
        }

        private static IBenzeneResult<T> ReturnObjectResult<T>(string status, string body, ISerializer serializer)
        {
            if (BenzeneResultHttpMapper.IsSuccessStatus(status))
            {
                return BenzeneResult.Set(status, serializer.Deserialize<T>(body));
            }

            var errorPayload = serializer.Deserialize<ErrorPayload>(body);
            return string.IsNullOrEmpty(errorPayload?.Detail)
                ? BenzeneResult.Set<T>(status, false)
                : BenzeneResult.Set<T>(status, errorPayload.Detail);
        }

        private static IBenzeneResult<T> ReturnGuidResult<T>(string status, string body, ISerializer serializer)
        {
            if (BenzeneResultHttpMapper.IsSuccessStatus(status))
            {
                return (IBenzeneResult<T>)BenzeneResult.Set(status, ParseGuid(body, serializer));
            }

            var errorPayload = serializer.Deserialize<ErrorPayload>(body);
            return string.IsNullOrEmpty(errorPayload?.Detail)
                ? BenzeneResult.Set<T>(status, false)
                : BenzeneResult.Set<T>(status, errorPayload.Detail);
        }

        private static Guid ParseGuid(string body, ISerializer serializer)
        {
            var successfullyParsed = Guid.TryParse(serializer.Deserialize<string>(body), out var parsedGuid);

            if (!successfullyParsed)
            {
                parsedGuid = Guid.Empty;
            }

            return parsedGuid;
        }
    }
}
