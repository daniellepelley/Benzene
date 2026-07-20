using System;
using Benzene.Abstractions.DI;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.Grpc.Client;
using Benzene.Grpc.Serialization;
using Benzene.Grpc.Test.Helpers;
using Benzene.Grpc.Test.Protos;
using Benzene.Grpc.TestHelpers;
using Benzene.Results;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Benzene.Grpc.Test;

public class GrpcBenzeneMessageClientTest
{
    [Fact]
    public async Task SendMessageAsync_WhenTheCallSucceeds_ReturnsTheMappedResponse()
    {
        var invoker = new TestCallInvoker { Response = new EchoReply { Message = "hello" } };
        var client = BuildClient(invoker, out var registry);
        registry.Add<EchoRequest, EchoReply>("echo-topic", "/benzene.test.TestService/Echo");

        var result = await client.SendMessageAsync<EchoRequest, EchoReply>(
            new BenzeneClientRequest<EchoRequest>("echo-topic", new EchoRequest { Name = "world" }, new Dictionary<string, string>()));

        Assert.True(result.IsSuccessful);
        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
        Assert.Equal("hello", result.Payload.Message);
    }

    [Fact]
    public async Task SendMessageAsync_WhenTheCallFailsWithRpcException_ReturnsTheMappedErrorStatus()
    {
        var invoker = new TestCallInvoker { RpcExceptionToThrow = new RpcException(new Status(StatusCode.NotFound, "no such thing")) };
        var client = BuildClient(invoker, out var registry);
        registry.Add<EchoRequest, EchoReply>("echo-topic", "/benzene.test.TestService/Echo");

        var result = await client.SendMessageAsync<EchoRequest, EchoReply>(
            new BenzeneClientRequest<EchoRequest>("echo-topic", new EchoRequest { Name = "world" }, new Dictionary<string, string>()));

        Assert.False(result.IsSuccessful);
        Assert.Equal(BenzeneResultStatus.NotFound, result.Status);
        Assert.Contains("no such thing", result.Errors);
    }

    [Fact]
    public async Task SendMessageAsync_WhenNoRouteIsRegistered_ReturnsNotImplemented()
    {
        var invoker = new TestCallInvoker();
        var client = BuildClient(invoker, out _);

        var result = await client.SendMessageAsync<EchoRequest, EchoReply>(
            new BenzeneClientRequest<EchoRequest>("unregistered-topic", new EchoRequest { Name = "world" }, new Dictionary<string, string>()));

        Assert.False(result.IsSuccessful);
        Assert.Equal(BenzeneResultStatus.NotImplemented, result.Status);
    }

    [Fact]
    public async Task SendMessageAsync_PropagatesInboundGrpcDeadlineToTheDownstreamCall()
    {
        var deadline = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var accessor = new GrpcServerCallAccessor { CallContext = TestServerCallContext.Create(deadline: deadline) };
        var resolver = new Mock<IServiceResolver>();
        resolver.Setup(x => x.TryGetService<IGrpcServerCallAccessor>()).Returns(accessor);

        var invoker = new TestCallInvoker { Response = new EchoReply { Message = "hello" } };
        var client = BuildClient(invoker, out var registry, resolver.Object);
        registry.Add<EchoRequest, EchoReply>("echo-topic", "/benzene.test.TestService/Echo");

        await client.SendMessageAsync<EchoRequest, EchoReply>(
            new BenzeneClientRequest<EchoRequest>("echo-topic", new EchoRequest { Name = "world" }, new Dictionary<string, string>()));

        // The downstream call inherits the same absolute wall-clock deadline (deadline propagation).
        Assert.Equal(deadline, invoker.CapturedOptions.Deadline);
    }

    [Fact]
    public async Task SendMessageAsync_NoInboundCall_ForwardsNoDeadline()
    {
        var invoker = new TestCallInvoker { Response = new EchoReply { Message = "hello" } };
        var client = BuildClient(invoker, out var registry);
        registry.Add<EchoRequest, EchoReply>("echo-topic", "/benzene.test.TestService/Echo");

        await client.SendMessageAsync<EchoRequest, EchoReply>(
            new BenzeneClientRequest<EchoRequest>("echo-topic", new EchoRequest { Name = "world" }, new Dictionary<string, string>()));

        Assert.Null(invoker.CapturedOptions.Deadline);
    }

    private static GrpcBenzeneMessageClient BuildClient(TestCallInvoker invoker, out GrpcClientRouteRegistry registry, IServiceResolver? resolver = null)
    {
        registry = new GrpcClientRouteRegistry();
        var adapter = new ProtobufJsonGrpcMessageAdapter();

        var pipeline = new MiddlewarePipelineBuilder<GrpcSendMessageContext>(new NullBenzeneServiceContainer())
            .UseGrpcClient(invoker, registry, adapter)
            .Build();

        return new GrpcBenzeneMessageClient(pipeline, adapter, new DefaultGrpcStatusReverseMapper(), NullLogger<GrpcBenzeneMessageClient>.Instance, resolver ?? new NullServiceResolver());
    }
}
