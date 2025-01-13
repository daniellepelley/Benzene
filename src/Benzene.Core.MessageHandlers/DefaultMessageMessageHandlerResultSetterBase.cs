using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;

namespace Benzene.Core.MessageHandlers;

public abstract class DefaultMessageMessageHandlerResultSetterBase<TContext>: IMessageHandlerResultSetter<TContext> 
{
    public Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        return Task.CompletedTask;
    }
}