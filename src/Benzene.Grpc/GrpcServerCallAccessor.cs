using Grpc.Core;

namespace Benzene.Grpc;

public class GrpcServerCallAccessor : IGrpcServerCallAccessor
{
    public ServerCallContext? CallContext { get; set; }

    public CancellationToken CancellationToken => CallContext?.CancellationToken ?? CancellationToken.None;
}
