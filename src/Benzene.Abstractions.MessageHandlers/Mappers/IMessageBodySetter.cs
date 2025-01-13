namespace Benzene.Abstractions.MessageHandlers.Mappers;

public interface IMessageBodySetter<TContext> 
{
    Task SetBody(TContext context, string body);
}