using Benzene.Abstractions.MessageHandling;

namespace Benzene.Abstractions.Mappers;

public interface IMessageTopicMapper<TContext>
{
    ITopic GetTopic(TContext context);
}