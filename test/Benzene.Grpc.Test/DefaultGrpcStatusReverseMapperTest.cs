using Benzene.Grpc.Client;
using Benzene.Results;
using Grpc.Core;
using Xunit;

namespace Benzene.Grpc.Test;

public class DefaultGrpcStatusReverseMapperTest
{
    public static IEnumerable<object[]> StatusMappings()
    {
        yield return new object[] { StatusCode.OK, BenzeneResultStatus.Ok };
        yield return new object[] { StatusCode.InvalidArgument, BenzeneResultStatus.BadRequest };
        yield return new object[] { StatusCode.Unauthenticated, BenzeneResultStatus.Unauthorized };
        yield return new object[] { StatusCode.PermissionDenied, BenzeneResultStatus.Forbidden };
        yield return new object[] { StatusCode.NotFound, BenzeneResultStatus.NotFound };
        yield return new object[] { StatusCode.AlreadyExists, BenzeneResultStatus.Conflict };
        yield return new object[] { StatusCode.Unimplemented, BenzeneResultStatus.NotImplemented };
        yield return new object[] { StatusCode.Unavailable, BenzeneResultStatus.ServiceUnavailable };
        yield return new object[] { StatusCode.ResourceExhausted, BenzeneResultStatus.TooManyRequests };
        yield return new object[] { StatusCode.DeadlineExceeded, BenzeneResultStatus.Timeout };
        yield return new object[] { StatusCode.Cancelled, BenzeneResultStatus.ServiceUnavailable };
    }

    [Theory]
    [MemberData(nameof(StatusMappings))]
    public void Map_MapsEveryKnownStatusCode(StatusCode statusCode, string expected)
    {
        var mapper = new DefaultGrpcStatusReverseMapper();

        Assert.Equal(expected, mapper.Map(statusCode, trailers: null));
    }

    [Fact]
    public void Map_WhenUnrecognized_ReturnsUnexpectedError()
    {
        var mapper = new DefaultGrpcStatusReverseMapper();

        Assert.Equal(BenzeneResultStatus.UnexpectedError, mapper.Map(StatusCode.DataLoss, trailers: null));
    }

    [Fact]
    public void Map_WhenBenzeneStatusTrailerIsPresent_ItWinsOverTheStatusCodeMapping()
    {
        var mapper = new DefaultGrpcStatusReverseMapper();
        var trailers = new Metadata { { "benzene-status", "created" } };

        Assert.Equal("created", mapper.Map(StatusCode.OK, trailers));
    }

    [Fact]
    public void Map_WhenTrailersDoNotContainTheBenzeneStatusKey_FallsBackToTheStatusCodeMapping()
    {
        var mapper = new DefaultGrpcStatusReverseMapper();
        var trailers = new Metadata { { "other-key", "value" } };

        Assert.Equal(BenzeneResultStatus.NotFound, mapper.Map(StatusCode.NotFound, trailers));
    }
}
