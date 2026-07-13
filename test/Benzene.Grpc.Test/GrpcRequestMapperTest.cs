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
}
