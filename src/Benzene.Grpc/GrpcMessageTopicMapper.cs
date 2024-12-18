using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.Mappers;

namespace Benzene.Grpc;

public class GrpcMessageTopicMapper : IMessageTopicMapper<GrpcContext>
{
    public ITopic GetTopic(GrpcContext context)
    {
        return new Topic(context.Topic);
    }
}