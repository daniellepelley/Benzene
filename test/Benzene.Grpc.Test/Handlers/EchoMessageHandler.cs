using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Grpc.Test.Protos;
using Benzene.Results;

namespace Benzene.Grpc.Test.Handlers;

[Message("grpc-test-echo-topic")]
public class EchoMessageHandler : IMessageHandler<EchoRequest, EchoReply>
{
    public Task<IBenzeneResult<EchoReply>> HandleAsync(EchoRequest request)
    {
        return Task.FromResult(BenzeneResult.Ok(new EchoReply { Message = $"Hello {request.Name}" }));
    }
}
