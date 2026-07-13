using Benzene.Grpc.Serialization;
using Benzene.Grpc.Test.Helpers;
using Benzene.Grpc.Test.Protos;
using Moq;
using Xunit;

namespace Benzene.Grpc.Test;

public class GrpcRequestMapperTest
{
    private class EchoRequestPoco
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void GetBody_WhenRequestAsObjectIsAlreadyTargetType_ReturnsSameInstanceWithoutCallingAdapter()
    {
        var adapter = new Mock<IGrpcMessageAdapter>(MockBehavior.Strict);
        var mapper = new GrpcRequestMapper(adapter.Object);
        var request = new EchoRequest { Name = "foo" };
        var context = new GrpcContext<EchoRequest, EchoReply>("topic", TestCallContext.Create(), request);

        var result = mapper.GetBody<EchoRequest>(context);

        Assert.Same(request, result);
        adapter.VerifyNoOtherCalls();
    }

    [Fact]
    public void GetBody_WhenRequestAsObjectIsNotTargetType_DelegatesToAdapter()
    {
        var converted = new EchoRequestPoco { Name = "foo" };
        var adapter = new Mock<IGrpcMessageAdapter>();
        adapter.Setup(x => x.ConvertRequest<EchoRequestPoco>(It.IsAny<object>())).Returns(converted);
        var mapper = new GrpcRequestMapper(adapter.Object);
        var request = new EchoRequest { Name = "foo" };
        var context = new GrpcContext<EchoRequest, EchoReply>("topic", TestCallContext.Create(), request);

        var result = mapper.GetBody<EchoRequestPoco>(context);

        Assert.Same(converted, result);
        adapter.Verify(x => x.ConvertRequest<EchoRequestPoco>(request), Times.Once);
    }

    [Fact]
    public void GetBody_WhenRequestAsObjectIsAnAssignableStream_ReturnsSameInstanceWithoutCallingAdapter()
    {
        var adapter = new Mock<IGrpcMessageAdapter>(MockBehavior.Strict);
        var mapper = new GrpcRequestMapper(adapter.Object);
        var items = AsAsyncEnumerable(new[] { new EchoRequest { Name = "a" }, new EchoRequest { Name = "b" } });
        var context = new GrpcContext<IAsyncEnumerable<EchoRequest>, EchoReply>("topic", TestCallContext.Create(), items);

        var result = mapper.GetBody<IAsyncEnumerable<EchoRequest>>(context);

        Assert.Same(items, result);
        adapter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetBody_WhenRequestStreamItemTypeDiffersFromTarget_WrapsEachItemViaAdapter()
    {
        var converted = new EchoRequestPoco { Name = "converted" };
        var adapter = new Mock<IGrpcMessageAdapter>();
        adapter.Setup(x => x.ConvertRequest<EchoRequestPoco>(It.IsAny<object>())).Returns(converted);
        var mapper = new GrpcRequestMapper(adapter.Object);
        var items = AsAsyncEnumerable(new[] { new EchoRequest { Name = "a" } });
        var context = new GrpcContext<IAsyncEnumerable<EchoRequest>, EchoReply>("topic", TestCallContext.Create(), items);

        var result = mapper.GetBody<IAsyncEnumerable<EchoRequestPoco>>(context);
        Assert.NotNull(result);

        var materialized = new List<EchoRequestPoco>();
        await foreach (var item in result!)
        {
            materialized.Add(item);
        }

        Assert.Single(materialized);
        Assert.Same(converted, materialized[0]);
        adapter.Verify(x => x.ConvertRequest<EchoRequestPoco>(It.IsAny<object>()), Times.Once);
    }

    private static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }
}
