using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Grpc.Test.Protos;
using Benzene.Results;

namespace Benzene.Grpc.Test.Handlers;

public class EchoReplyPoco
{
    public string Message { get; set; } = string.Empty;
}

[Message("grpc-test-echo-poco-topic")]
public class EchoPocoMessageHandler : IMessageHandler<EchoRequest, EchoReplyPoco>
{
    public Task<IBenzeneResult<EchoReplyPoco>> HandleAsync(EchoRequest request)
    {
        return Task.FromResult(BenzeneResult.Ok(new EchoReplyPoco { Message = $"Hello {request.Name}" }));
    }
}
