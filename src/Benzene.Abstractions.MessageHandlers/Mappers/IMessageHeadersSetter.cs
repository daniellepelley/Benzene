namespace Benzene.Abstractions.MessageHandlers.Mappers;

public interface IMessageHeadersSetter<TContext>
{
    Task SetHeaders(TContext context, IDictionary<string, string> headers);
}