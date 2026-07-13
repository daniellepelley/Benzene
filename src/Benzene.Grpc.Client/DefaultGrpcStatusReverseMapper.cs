using Benzene.Results;
using Grpc.Core;

namespace Benzene.Grpc.Client;

/// <summary>
/// Provides the default mapping from gRPC status codes back to Benzene result statuses. This is the
/// reverse of <c>Benzene.Grpc.DefaultGrpcStatusCodeMapper</c>; several Benzene statuses collapse to
/// <see cref="StatusCode.OK"/> forwards, so on the way back <see cref="StatusCode.OK"/> only ever
/// recovers as <see cref="BenzeneResultStatus.Ok"/> unless a <c>benzene-status</c> trailer says otherwise.
/// </summary>
public class DefaultGrpcStatusReverseMapper : IGrpcStatusReverseMapper
{
    private const string TrailerKey = "benzene-status";

    private readonly IDictionary<StatusCode, string> _dictionary;

    public DefaultGrpcStatusReverseMapper()
    {
        _dictionary = new Dictionary<StatusCode, string>
        {
            { StatusCode.OK, BenzeneResultStatus.Ok },
            { StatusCode.InvalidArgument, BenzeneResultStatus.BadRequest },
            { StatusCode.Unauthenticated, BenzeneResultStatus.Unauthorized },
            { StatusCode.PermissionDenied, BenzeneResultStatus.Forbidden },
            { StatusCode.NotFound, BenzeneResultStatus.NotFound },
            { StatusCode.AlreadyExists, BenzeneResultStatus.Conflict },
            { StatusCode.Unimplemented, BenzeneResultStatus.NotImplemented },
            { StatusCode.Unavailable, BenzeneResultStatus.ServiceUnavailable },
            { StatusCode.DeadlineExceeded, BenzeneResultStatus.ServiceUnavailable },
            { StatusCode.Cancelled, BenzeneResultStatus.ServiceUnavailable },
        };
    }

    public string Map(StatusCode statusCode, Metadata? trailers)
    {
        var trailerStatus = trailers?.FirstOrDefault(x => !x.IsBinary && x.Key == TrailerKey)?.Value;
        if (!string.IsNullOrEmpty(trailerStatus))
        {
            return trailerStatus;
        }

        return _dictionary.TryGetValue(statusCode, out var mapped) ? mapped : BenzeneResultStatus.UnexpectedError;
    }
}
