using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Grpc;
using Benzene.Grpc.Test.Protos;
using Benzene.Results;

namespace Benzene.Grpc.Test.Handlers;

[GrpcMethod("/benzene.test.TestService/Chat")]
[Message("grpc-test-chat-topic")]
public class ChatMessageHandler : IMessageHandler<IAsyncEnumerable<ChatMessage>, IAsyncEnumerable<ChatMessage>>
{
    public Task<IBenzeneResult<IAsyncEnumerable<ChatMessage>>> HandleAsync(IAsyncEnumerable<ChatMessage> request)
    {
        return Task.FromResult(BenzeneResult.Ok(Echo(request)));
    }

    private static async IAsyncEnumerable<ChatMessage> Echo(IAsyncEnumerable<ChatMessage> source)
    {
        await foreach (var message in source)
        {
            yield return new ChatMessage { Text = $"Echo: {message.Text}" };
        }
    }
}
