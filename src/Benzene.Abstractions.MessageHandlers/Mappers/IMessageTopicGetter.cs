using Benzene.Abstractions.Messages;

namespace Benzene.Abstractions.MessageHandlers.Mappers;

public interface IMessageTopicGetter<TContext>
{
    ITopic? GetTopic(TContext context);
}