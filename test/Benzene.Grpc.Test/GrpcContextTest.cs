using Benzene.Grpc;
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
        var context = new GrpcContext<string, EchoResponsePoco>("test-topic", "request");
        var response = new EchoResponsePoco { Message = "hello" };

        context.ResponseAsObject = response;

        Assert.Same(response, context.Response);
    }

    [Fact]
    public void ResponseAsObject_WhenValueIsNotTResponse_ConvertsViaJsonRoundTrip()
    {
        var context = new GrpcContext<string, EchoResponsePoco>("test-topic", "request");

        context.ResponseAsObject = new { Message = "hi" };

        Assert.NotNull(context.Response);
        Assert.Equal("hi", context.Response!.Message);
    }

    [Fact]
    public void RequestAsObject_ReturnsRequest()
    {
        var context = new GrpcContext<string, EchoResponsePoco>("test-topic", "request");

        Assert.Equal("request", context.RequestAsObject);
    }

    [Fact]
    public void Topic_IsSetFromConstructor()
    {
        var context = new GrpcContext<string, EchoResponsePoco>("test-topic", "request");

        Assert.Equal("test-topic", context.Topic);
    }
}
