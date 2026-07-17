using Benzene.Results;
using Grpc.Core;

namespace Benzene.Grpc;

/// <summary>
/// Provides the default mapping between Benzene result status codes and gRPC status codes.
/// </summary>
/// <remarks>
/// Unknown or null status values default to <see cref="StatusCode.Internal"/>.
/// </remarks>
public class DefaultGrpcStatusCodeMapper : IGrpcStatusCodeMapper
{
    private const StatusCode DefaultValue = StatusCode.Internal;
    private readonly IDictionary<string, StatusCode> _dictionary;

    public DefaultGrpcStatusCodeMapper()
    {
        _dictionary = new Dictionary<string, StatusCode>
        {
            { BenzeneResultStatus.Ok, StatusCode.OK },
            { BenzeneResultStatus.Ignored, StatusCode.OK },
            { BenzeneResultStatus.Created, StatusCode.OK },
            { BenzeneResultStatus.Accepted, StatusCode.OK },
            { BenzeneResultStatus.Updated, StatusCode.OK },
            { BenzeneResultStatus.Deleted, StatusCode.OK },
            { BenzeneResultStatus.BadRequest, StatusCode.InvalidArgument },
            { BenzeneResultStatus.ValidationError, StatusCode.InvalidArgument },
            { BenzeneResultStatus.Unauthorized, StatusCode.Unauthenticated },
            { BenzeneResultStatus.Forbidden, StatusCode.PermissionDenied },
            { BenzeneResultStatus.NotFound, StatusCode.NotFound },
            { BenzeneResultStatus.Conflict, StatusCode.AlreadyExists },
            { BenzeneResultStatus.NotImplemented, StatusCode.Unimplemented },
            { BenzeneResultStatus.ServiceUnavailable, StatusCode.Unavailable },
            { BenzeneResultStatus.TooManyRequests, StatusCode.ResourceExhausted },
            { BenzeneResultStatus.Timeout, StatusCode.DeadlineExceeded },
            { BenzeneResultStatus.UnexpectedError, StatusCode.Internal }
        };
    }

    public StatusCode Map(string? benzeneResultStatus)
    {
        if (benzeneResultStatus == null)
        {
            return DefaultValue;
        }

        return _dictionary.TryGetValue(benzeneResultStatus, out var mapped)
            ? mapped
            : DefaultValue;
    }
}
