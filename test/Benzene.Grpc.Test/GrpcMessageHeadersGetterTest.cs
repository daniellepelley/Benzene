using Benzene.Grpc.TestHelpers;
using Grpc.Core;
using Xunit;

namespace Benzene.Grpc.Test;

public class GrpcMessageHeadersGetterTest
{
    [Fact]
    public void GetHeaders_MapsNonBinaryRequestHeaders()
    {
        var requestHeaders = new Metadata
        {
            { "x-correlation-id", "abc-123" },
            { "x-trace-id", "trace-456" }
        };
        var context = new GrpcContext<string, string>("topic", TestServerCallContext.Create(requestHeaders: requestHeaders), "request");
        var getter = new GrpcMessageHeadersGetter();

        var headers = getter.GetHeaders(context);

        Assert.Equal("abc-123", headers["x-correlation-id"]);
        Assert.Equal("trace-456", headers["x-trace-id"]);
    }

    [Fact]
    public void GetHeaders_SkipsBinaryHeaders()
    {
        var requestHeaders = new Metadata
        {
            { "x-plain", "value" },
            { "x-binary-bin", new byte[] { 1, 2, 3 } }
        };
        var context = new GrpcContext<string, string>("topic", TestServerCallContext.Create(requestHeaders: requestHeaders), "request");
        var getter = new GrpcMessageHeadersGetter();

        var headers = getter.GetHeaders(context);

        Assert.Equal("value", headers["x-plain"]);
        Assert.False(headers.ContainsKey("x-binary-bin"));
    }

    [Fact]
    public void GetHeaders_WhenDuplicateKeys_LastWins()
    {
        var requestHeaders = new Metadata
        {
            { "x-header", "first" },
            { "x-header", "second" }
        };
        var context = new GrpcContext<string, string>("topic", TestServerCallContext.Create(requestHeaders: requestHeaders), "request");
        var getter = new GrpcMessageHeadersGetter();

        var headers = getter.GetHeaders(context);

        Assert.Equal("second", headers["x-header"]);
    }
}
