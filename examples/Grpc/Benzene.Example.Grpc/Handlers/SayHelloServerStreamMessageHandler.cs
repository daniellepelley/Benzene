using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Grpc;
using Benzene.Results;

namespace Benzene.Example.Grpc.Handlers;

/// <summary>
/// Declares the generated protobuf types directly (<see cref="HelloRequest"/>/<see cref="HelloReply"/>)
/// rather than POCOs - a zero-copy alternative to <see cref="SayHelloMessageHandler"/>'s JSON-bridged
/// style, available for either side of any RPC shape.
/// </summary>
[GrpcMethod("/greet.Greeter/SayHelloServerStream")]
[Message("say_hello_server_stream")]
public class SayHelloServerStreamMessageHandler : IMessageHandler<HelloRequest, IAsyncEnumerable<HelloReply>>
{
    private static readonly string[] Salutations = { "Hello", "Hi", "Hey" };

    public Task<IBenzeneResult<IAsyncEnumerable<HelloReply>>> HandleAsync(HelloRequest request)
    {
        return BenzeneResult.Ok(Produce(request.Name)).AsTask();
    }

    private static async IAsyncEnumerable<HelloReply> Produce(string name)
    {
        foreach (var salutation in Salutations)
        {
            yield return new HelloReply { Message = $"{salutation} {name}" };
            await Task.Yield();
        }
    }
}
