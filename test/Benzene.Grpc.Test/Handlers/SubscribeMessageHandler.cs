using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Grpc;
using Benzene.Grpc.Test.Protos;
using Benzene.Results;

namespace Benzene.Grpc.Test.Handlers;

[GrpcMethod("/benzene.test.TestService/Subscribe")]
[Message("grpc-test-subscribe-topic")]
public class SubscribeMessageHandler : IMessageHandler<SubscribeRequest, IAsyncEnumerable<SubscribeReply>>
{
    public Task<IBenzeneResult<IAsyncEnumerable<SubscribeReply>>> HandleAsync(SubscribeRequest request)
    {
        return Task.FromResult(BenzeneResult.Ok(Produce(request.Topic)));
    }

    private static async IAsyncEnumerable<SubscribeReply> Produce(string topic)
    {
        for (var i = 0; i < 3; i++)
        {
            yield return new SubscribeReply { Item = $"{topic}-{i}" };
            await Task.Yield();
        }
    }
}
