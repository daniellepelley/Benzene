namespace Benzene.Abstractions.Messages.Mappers;

public interface IMessageBodySetter<TContext> 
{
    Task SetBody(TContext context, string body);
}