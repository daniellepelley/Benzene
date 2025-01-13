namespace Benzene.Abstractions.MessageHandlers.Mappers;

public interface IMessageHeadersGetter<TContext>
{
    IDictionary<string, string> GetHeaders(TContext context);
}