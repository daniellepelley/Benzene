namespace Benzene.Abstractions.MessageHandlers.Mappers;

public interface IResultSetter<TContext>
{
    Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult);
}