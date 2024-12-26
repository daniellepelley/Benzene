namespace Benzene.Abstractions.Mappers;

public interface IMessageHeadersMapper<TContext>
{
    IDictionary<string, string> GetHeaders(TContext context);
}