namespace Benzene.Abstractions.Mappers;

public interface IMessageBodyMapper<TContext> 
{
    string? GetBody(TContext context);
}
