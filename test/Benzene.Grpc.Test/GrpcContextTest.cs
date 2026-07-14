using Benzene.Grpc;
using Benzene.Grpc.TestHelpers;
using Xunit;

namespace Benzene.Grpc.Test;

public class GrpcContextTest
{
    private class EchoResponsePoco
    {
        public string Message { get; set; } = string.Empty;
    }

    [Fact]
    public void ResponseAsObject_WhenValueIsTResponse_AssignsSameInstance()
    {
        var context = new GrpcContext<string, EchoResponsePoco>("test-topic", TestServerCallContext.Create(), "request");
        var response = new EchoResponsePoco { Message = "hello" };

        context.ResponseAsObject = response;

        Assert.Same(response, context.Response);
    }

    [Fact]
    public void ResponseAsObject_WhenValueIsNotTResponse_SetsResponsePayloadInsteadOfResponse()
    {
        var context = new GrpcContext<string, EchoResponsePoco>("test-topic", TestServerCallContext.Create(), "request");
        var payload = new { Message = "hi" };

        context.ResponseAsObject = payload;

        Assert.Null(context.Response);
        Assert.Same(payload, context.ResponsePayload);
    }

    [Fact]
    public void ResponseAsObject_Getter_FallsBackToResponsePayloadWhenResponseNotSet()
    {
        var context = new GrpcContext<string, EchoResponsePoco>("test-topic", TestServerCallContext.Create(), "request");
        var payload = new { Message = "hi" };

        context.ResponseAsObject = payload;

        Assert.Same(payload, context.ResponseAsObject);
    }

    [Fact]
    public void RequestAsObject_ReturnsRequest()
    {
        var context = new GrpcContext<string, EchoResponsePoco>("test-topic", TestServerCallContext.Create(), "request");

        Assert.Equal("request", context.RequestAsObject);
    }

    [Fact]
    public void Topic_IsSetFromConstructor()
    {
        var context = new GrpcContext<string, EchoResponsePoco>("test-topic", TestServerCallContext.Create(), "request");

        Assert.Equal("test-topic", context.Topic);
    }
}
