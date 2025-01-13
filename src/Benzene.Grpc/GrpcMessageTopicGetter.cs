using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;

namespace Benzene.Grpc;

public class GrpcMessageTopicGetter : IMessageTopicGetter<GrpcContext>
{
    public ITopic GetTopic(GrpcContext context)
    {
        return new Topic(context.Topic);
    }
}
