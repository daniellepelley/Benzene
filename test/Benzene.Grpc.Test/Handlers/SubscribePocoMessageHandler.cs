using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Grpc.Test.Protos;
using Benzene.Results;

namespace Benzene.Grpc.Test.Handlers;

public class SubscribeItemPoco
{
    public string Item { get; set; } = string.Empty;
}

/// <summary>
/// Declares a POCO stream item type rather than the protobuf <see cref="SubscribeReply"/> directly, exercising
/// <c>GrpcStreamAdapter</c>'s per-item converting wrapper on the response-stream side. Not routed via
/// <see cref="GrpcMethodAttribute"/> (like <see cref="EchoPocoMessageHandler"/>) - exercised directly by tests
/// that construct a <see cref="GrpcMethodHandler"/> against this topic.
/// </summary>
[Message("grpc-test-subscribe-poco-topic")]
public class SubscribePocoMessageHandler : IMessageHandler<SubscribeRequest, IAsyncEnumerable<SubscribeItemPoco>>
{
    public Task<IBenzeneResult<IAsyncEnumerable<SubscribeItemPoco>>> HandleAsync(SubscribeRequest request)
    {
        return Task.FromResult(BenzeneResult.Ok(Produce(request.Topic)));
    }

    private static async IAsyncEnumerable<SubscribeItemPoco> Produce(string topic)
    {
        for (var i = 0; i < 3; i++)
        {
            yield return new SubscribeItemPoco { Item = $"{topic}-{i}" };
            await Task.Yield();
        }
    }
}
