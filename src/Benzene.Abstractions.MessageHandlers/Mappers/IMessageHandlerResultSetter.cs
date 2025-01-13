namespace Benzene.Abstractions.MessageHandlers.Mappers;

public interface IMessageHandlerResultSetter<TContext>
{
    Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult);
}