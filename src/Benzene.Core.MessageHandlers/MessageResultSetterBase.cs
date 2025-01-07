using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.ToDelete;

namespace Benzene.Core.MessageHandlers;

public abstract class MessageResultSetterBase<TContext>: IResultSetter<TContext> where TContext : IHasMessageResult
{
    public Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        context.MessageResult = new MessageResult(messageHandlerResult.BenzeneResult.IsSuccessful);
        return Task.CompletedTask;
    }
}