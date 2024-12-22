using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Abstractions.Mappers;

public interface IResultSetter<TContext>
{
    Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult);
}