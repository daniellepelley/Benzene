using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Grpc;
using Benzene.Grpc.Test.Protos;
using Benzene.Results;

namespace Benzene.Grpc.Test.Handlers;

[Message("grpc-test-accessor-topic")]
public class AccessorAwareMessageHandler : IMessageHandler<EchoRequest, EchoReply>
{
    private readonly IGrpcServerCallAccessor _accessor;

    public AccessorAwareMessageHandler(IGrpcServerCallAccessor accessor)
    {
        _accessor = accessor;
    }

    public Task<IBenzeneResult<EchoReply>> HandleAsync(EchoRequest request)
    {
        var message = _accessor.CancellationToken.IsCancellationRequested ? "cancelled" : "not-cancelled";
        return Task.FromResult(BenzeneResult.Ok(new EchoReply { Message = message }));
    }
}
