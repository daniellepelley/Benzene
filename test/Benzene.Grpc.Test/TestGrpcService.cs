using Benzene.Grpc.Test.Protos;
using Grpc.Core;

namespace Benzene.Grpc.Test;

/// <summary>
/// The native gRPC service implementation for <see cref="TestService"/>. <see cref="BenzeneInterceptor"/>
/// substitutes Benzene's own handling for any method matched by a registered <c>[GrpcMethod]</c> route;
/// this class's overrides are only reached for methods that aren't routed to a Benzene handler.
/// </summary>
public class TestGrpcService : TestService.TestServiceBase
{
    public override Task<EchoReply> Echo(EchoRequest request, ServerCallContext context)
    {
        return Task.FromResult(new EchoReply { Message = $"Native:{request.Name}" });
    }
}
