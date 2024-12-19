using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Abstractions.Mappers;

public interface IMessageTopicMapper<TContext>
{
    ITopic? GetTopic(TContext context);
}