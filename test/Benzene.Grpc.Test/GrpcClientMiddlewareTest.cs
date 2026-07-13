using Benzene.Grpc.Client;
using Benzene.Grpc.Serialization;
using Benzene.Grpc.Test.Helpers;
using Benzene.Grpc.Test.Protos;
using Grpc.Core;
using Xunit;

namespace Benzene.Grpc.Test;

public class GrpcClientMiddlewareTest
{
    [Fact]
    public async Task HandleAsync_WhenRouteIsRegistered_InvokesItWithTheRequestAndHeaders()
    {
        var invoker = new TestCallInvoker { Response = new EchoReply { Message = "hello" } };
        var registry = new GrpcClientRouteRegistry();
        registry.Add<EchoRequest, EchoReply>("echo-topic", "/benzene.test.TestService/Echo");
        var middleware = new GrpcClientMiddleware(invoker, registry, new ProtobufJsonGrpcMessageAdapter());
        var headers = new Metadata { { "x-correlation-id", "abc-123" } };
        var context = new GrpcSendMessageContext("echo-topic", new EchoRequest { Name = "world" }, headers, deadline: null, CancellationToken.None);

        await middleware.HandleAsync(context, () => Task.CompletedTask);

        Assert.Equal("/benzene.test.TestService/Echo", invoker.CapturedMethod?.FullName);
        Assert.Same(headers, invoker.CapturedOptions.Headers);
        Assert.Equal("world", Assert.IsType<EchoRequest>(invoker.CapturedRequest).Name);
        Assert.Equal("hello", Assert.IsType<EchoReply>(context.Response).Message);
        Assert.Equal(StatusCode.OK, context.Status.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenNoRouteIsRegistered_SetsUnimplementedStatusWithoutCallingTheInvoker()
    {
        var invoker = new TestCallInvoker();
        var registry = new GrpcClientRouteRegistry();
        var middleware = new GrpcClientMiddleware(invoker, registry, new ProtobufJsonGrpcMessageAdapter());
        var context = new GrpcSendMessageContext("unregistered-topic", new EchoRequest(), new Metadata(), deadline: null, CancellationToken.None);

        await middleware.HandleAsync(context, () => Task.CompletedTask);

        Assert.Equal(StatusCode.Unimplemented, context.Status.StatusCode);
        Assert.Null(invoker.CapturedMethod);
    }

    [Fact]
    public async Task HandleAsync_WhenTheCallThrowsRpcException_CapturesTheStatusAndTrailersWithoutRethrowing()
    {
        var trailers = new Metadata { { "benzene-status", "NotFound" } };
        var invoker = new TestCallInvoker { RpcExceptionToThrow = new RpcException(new Status(StatusCode.NotFound, "missing"), trailers) };
        var registry = new GrpcClientRouteRegistry();
        registry.Add<EchoRequest, EchoReply>("echo-topic", "/benzene.test.TestService/Echo");
        var middleware = new GrpcClientMiddleware(invoker, registry, new ProtobufJsonGrpcMessageAdapter());
        var context = new GrpcSendMessageContext("echo-topic", new EchoRequest { Name = "world" }, new Metadata(), deadline: null, CancellationToken.None);

        await middleware.HandleAsync(context, () => Task.CompletedTask);

        Assert.Equal(StatusCode.NotFound, context.Status.StatusCode);
        Assert.Contains(context.ResponseTrailers!, e => e.Key == "benzene-status" && e.Value == "NotFound");
    }
}
