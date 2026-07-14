using Benzene.Core.Exceptions;
using Benzene.Grpc.Serialization;
using Benzene.Grpc.Test.Protos;
using Xunit;

namespace Benzene.Grpc.Test.Serialization;

public class ProtobufJsonGrpcMessageAdapterTest
{
    private class EchoRequestPoco
    {
        public string Name { get; set; } = string.Empty;
    }

    private class EchoReplyPoco
    {
        public string? Message { get; set; }
    }

    [Fact]
    public void ConvertRequest_WhenAlreadyTargetType_ReturnsSameInstance()
    {
        var adapter = new ProtobufJsonGrpcMessageAdapter();
        var request = new EchoRequest { Name = "foo" };

        var result = adapter.ConvertRequest<EchoRequest>(request);

        Assert.Same(request, result);
    }

    [Fact]
    public void ConvertRequest_WhenNull_ReturnsNull()
    {
        var adapter = new ProtobufJsonGrpcMessageAdapter();

        var result = adapter.ConvertRequest<EchoRequest>(null);

        Assert.Null(result);
    }

    [Fact]
    public void ConvertRequest_WhenProtobufMessage_ConvertsToPoco()
    {
        var adapter = new ProtobufJsonGrpcMessageAdapter();
        var request = new EchoRequest { Name = "foo" };

        var result = adapter.ConvertRequest<EchoRequestPoco>(request);

        Assert.NotNull(result);
        Assert.Equal("foo", result!.Name);
    }

    [Fact]
    public void ConvertResponse_WhenAlreadyTargetType_ReturnsSameInstance()
    {
        var adapter = new ProtobufJsonGrpcMessageAdapter();
        var reply = new EchoReply { Message = "hi" };

        var result = adapter.ConvertResponse<EchoReply>(reply);

        Assert.Same(reply, result);
    }

    [Fact]
    public void ConvertResponse_WhenPoco_ConvertsToProtobufMessage()
    {
        var adapter = new ProtobufJsonGrpcMessageAdapter();
        var payload = new EchoReplyPoco { Message = "hi" };

        var result = adapter.ConvertResponse<EchoReply>(payload);

        Assert.Equal("hi", result.Message);
    }

    [Fact]
    public void ConvertResponse_WhenPocoHasNullProperty_UsesProtobufDefault()
    {
        var adapter = new ProtobufJsonGrpcMessageAdapter();
        var payload = new EchoReplyPoco { Message = null };

        var result = adapter.ConvertResponse<EchoReply>(payload);

        Assert.Equal(string.Empty, result.Message);
    }

    [Fact]
    public void ConvertResponse_WhenPayloadIsNull_ThrowsBenzeneException()
    {
        var adapter = new ProtobufJsonGrpcMessageAdapter();

        Assert.Throws<BenzeneException>(() => adapter.ConvertResponse<EchoReply>(null));
    }

    [Fact]
    public void ConvertResponse_WhenTargetIsNotAProtobufMessage_ThrowsBenzeneException()
    {
        var adapter = new ProtobufJsonGrpcMessageAdapter();

        Assert.Throws<BenzeneException>(() => adapter.ConvertResponse<EchoReplyPoco>(new object()));
    }
}
