using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Grpc;
using Benzene.Results;

namespace Benzene.Example.Grpc.Handlers;

[GrpcMethod("/greet.Greeter/SayHelloBidiStream")]
[Message("say_hello_bidi_stream")]
public class SayHelloBidiStreamMessageHandler : IMessageHandler<IAsyncEnumerable<HelloRequest>, IAsyncEnumerable<HelloReply>>
{
    public Task<IBenzeneResult<IAsyncEnumerable<HelloReply>>> HandleAsync(IAsyncEnumerable<HelloRequest> request)
    {
        return BenzeneResult.Ok(Produce(request)).AsTask();
    }

    private static async IAsyncEnumerable<HelloReply> Produce(IAsyncEnumerable<HelloRequest> source)
    {
        await foreach (var item in source)
        {
            yield return new HelloReply { Message = $"Hello {item.Name}" };
        }
    }
}
