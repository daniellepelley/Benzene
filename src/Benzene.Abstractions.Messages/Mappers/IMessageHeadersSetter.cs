namespace Benzene.Abstractions.Messages.Mappers;

public interface IMessageHeadersSetter<TContext>
{
    Task SetHeaders(TContext context, IDictionary<string, string> headers);
}