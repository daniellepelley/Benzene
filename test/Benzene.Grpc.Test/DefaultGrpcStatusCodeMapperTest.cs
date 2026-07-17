using Benzene.Results;
using Grpc.Core;
using Xunit;

namespace Benzene.Grpc.Test;

public class DefaultGrpcStatusCodeMapperTest
{
    public static IEnumerable<object[]> StatusMappings()
    {
        yield return new object[] { BenzeneResultStatus.Ok, StatusCode.OK };
        yield return new object[] { BenzeneResultStatus.Ignored, StatusCode.OK };
        yield return new object[] { BenzeneResultStatus.Created, StatusCode.OK };
        yield return new object[] { BenzeneResultStatus.Accepted, StatusCode.OK };
        yield return new object[] { BenzeneResultStatus.Updated, StatusCode.OK };
        yield return new object[] { BenzeneResultStatus.Deleted, StatusCode.OK };
        yield return new object[] { BenzeneResultStatus.BadRequest, StatusCode.InvalidArgument };
        yield return new object[] { BenzeneResultStatus.ValidationError, StatusCode.InvalidArgument };
        yield return new object[] { BenzeneResultStatus.Unauthorized, StatusCode.Unauthenticated };
        yield return new object[] { BenzeneResultStatus.Forbidden, StatusCode.PermissionDenied };
        yield return new object[] { BenzeneResultStatus.NotFound, StatusCode.NotFound };
        yield return new object[] { BenzeneResultStatus.Conflict, StatusCode.AlreadyExists };
        yield return new object[] { BenzeneResultStatus.NotImplemented, StatusCode.Unimplemented };
        yield return new object[] { BenzeneResultStatus.ServiceUnavailable, StatusCode.Unavailable };
        yield return new object[] { BenzeneResultStatus.TooManyRequests, StatusCode.ResourceExhausted };
        yield return new object[] { BenzeneResultStatus.Timeout, StatusCode.DeadlineExceeded };
        yield return new object[] { BenzeneResultStatus.UnexpectedError, StatusCode.Internal };
    }

    [Theory]
    [MemberData(nameof(StatusMappings))]
    public void Map_MapsEveryKnownBenzeneResultStatus(string benzeneResultStatus, StatusCode expected)
    {
        var mapper = new DefaultGrpcStatusCodeMapper();

        Assert.Equal(expected, mapper.Map(benzeneResultStatus));
    }

    [Fact]
    public void Map_WhenNull_ReturnsInternal()
    {
        var mapper = new DefaultGrpcStatusCodeMapper();

        Assert.Equal(StatusCode.Internal, mapper.Map(null));
    }

    [Fact]
    public void Map_WhenUnrecognized_ReturnsInternal()
    {
        var mapper = new DefaultGrpcStatusCodeMapper();

        Assert.Equal(StatusCode.Internal, mapper.Map("SomethingUnknown"));
    }
}
