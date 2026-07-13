using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Grpc;
using Benzene.Results;

namespace Benzene.Example.Grpc.Handlers;

[GrpcMethod("/greet.Greeter/SayHelloClientStream")]
[Message("say_hello_client_stream")]
public class SayHelloClientStreamMessageHandler : IMessageHandler<IAsyncEnumerable<HelloRequest>, HelloReply>
{
    public async Task<IBenzeneResult<HelloReply>> HandleAsync(IAsyncEnumerable<HelloRequest> request)
    {
        var names = new List<string>();
        await foreach (var item in request)
        {
            names.Add(item.Name);
        }

        return BenzeneResult.Ok(new HelloReply { Message = $"Hello {string.Join(", ", names)}" });
    }
}
