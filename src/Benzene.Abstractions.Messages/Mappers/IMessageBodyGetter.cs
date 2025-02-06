namespace Benzene.Abstractions.Messages.Mappers;

public interface IMessageBodyGetter<TContext> 
{
    string? GetBody(TContext context);
}