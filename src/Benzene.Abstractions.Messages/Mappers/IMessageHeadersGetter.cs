namespace Benzene.Abstractions.Messages.Mappers;

public interface IMessageHeadersGetter<TContext>
{
    IDictionary<string, string> GetHeaders(TContext context);
}