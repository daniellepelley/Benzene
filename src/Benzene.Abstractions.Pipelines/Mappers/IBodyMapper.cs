namespace Benzene.Abstractions.Mappers;

public interface IMessageBodyMapper<TContext> 
{
    string? GetMessage(TContext context);
}
