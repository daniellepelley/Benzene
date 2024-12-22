using Benzene.Abstractions.MessageHandlers;
using Benzene.Results;

namespace Benzene.Abstractions.Mappers;

public interface IResultSetter<TContext>
{
    void SetResult(TContext context, IResult result, ITopic? topic, IMessageHandlerDefinition? messageHandlerDefinition);
}