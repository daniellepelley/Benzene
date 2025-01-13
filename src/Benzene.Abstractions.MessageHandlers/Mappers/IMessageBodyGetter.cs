namespace Benzene.Abstractions.MessageHandlers.Mappers;

public interface IMessageBodyGetter<TContext> 
{
    string? GetBody(TContext context);
}